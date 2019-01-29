using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Nest;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LogLevel = NLog.LogLevel;

namespace Host.Logger
{
  public class ResponseLogMiddleware
  {
    private readonly RequestDelegate _next;

    public ResponseLogMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task Invoke(HttpContext context)
    {

      bool logRequest = true;

      var skipRequests = GetDisallowedRequests();
      if (skipRequests.Any(x => context.Request.Path.ToString().Contains(x)))
      {
        logRequest = false;
      }

      int statusCode;
      long duration;

      var logger = LogManager.GetCurrentClassLogger();
      string requestBody = string.Empty;

      requestBody = await FormatRequest(context.Request);

      var originalBodyStream = context.Response.Body;

      string responseContent = string.Empty;

      using (var responseBody = new MemoryStream())
      {
        context.Response.Body = responseBody;

        var sw = Stopwatch.StartNew();

        await _next(context);
        duration = sw.ElapsedMilliseconds;

        responseContent = await FormatResponse(context.Response);
        await responseBody.CopyToAsync(originalBodyStream);
      }

      statusCode = context.Response.StatusCode;

      var contextFeature = context.Features.Get<IExceptionHandlerFeature>();

      var headers = context.Request.Headers;

      var authorization = ((Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestHeaders)headers).HeaderAuthorization;

      var authHeader = !string.IsNullOrEmpty(authorization) ? authorization.ToString() : "Unknown";

      var userInfo = context.User.Claims.Where(x => x.Type == "unique_name").FirstOrDefault();
      var clientInfo = context.User.Claims.Where(x => x.Type == "client_id").FirstOrDefault();

      string clientId = string.Empty;
      if (clientInfo != null)
      {
        clientId = clientInfo.Value;
      }
      string user = string.Empty;
      if (userInfo != null)
      {
        user = userInfo.Value;
        GlobalDiagnosticsContext.Set("user", userInfo.Value);
      }    

      if (logRequest)
      {
        //default level
        var level = LogLevel.Debug;

        if (statusCode >= 500)
        {
          // server error
          level = LogLevel.Error;
        }

        if (statusCode >= 200 && statusCode <= 399)
        {
          // informational, success or redirection
          level = LogLevel.Info;
        }

        if (statusCode >= 400 && statusCode <= 499)
        {
          // client error
          level = LogLevel.Warn;
        }
        var eventInfo = new LogEventInfo
        {
          Level = level,
          Message = string.Format(CultureInfo.InvariantCulture, "{0}", statusCode + " " + ResolveReasonPhrase(statusCode)),
          TimeStamp = DateTime.Now
        };

        string request = context.Request.Method + " " + context.Request.Host + context.Request.Path + context.Request.QueryString;
        string requestContent = requestBody;

        if (!string.IsNullOrEmpty(requestContent))
        {
          if (requestContent.Contains("password="))
          {
            requestContent = HideRequestPassword(requestContent);
          }

          eventInfo.Properties["requestContent"] = requestContent;
        }

        eventInfo.Properties["responseContent"] = responseContent.FormatLogContent();

        eventInfo.Properties["request"] = request;
        eventInfo.Properties["duration"] = duration;
        eventInfo.Properties["serverName"] = Environment.MachineName;
        eventInfo.Properties["authorizationHeader"] = authHeader;
        eventInfo.Properties["clientId"] = clientId;

        logger.Log(eventInfo);

        if (ElasticSettings.IsOn)
        {
          var node = new Uri(ElasticSettings.Host);
          var settings = new ConnectionSettings(node);
          var client = new ElasticClient(settings);

          var elasticLogger = new ElasticLogger
          {
            Timestamp = DateTime.UtcNow,
            User = user,
            ServerName = Environment.MachineName,
            Duration = duration.ToString(),
            Source = "Demo Api",
            Message = string.Format(CultureInfo.InvariantCulture, "{0}", statusCode + " " + ResolveReasonPhrase(statusCode)),
            Request = request,
            RequestContent = requestContent,
            ResponseContent = responseContent,
            ClientId = clientId  
          };

          await client.IndexAsync(elasticLogger, idx => idx.Index("api_logger"));
        }

      }
    }

    private static string ResolveRequestHeaders(HttpRequestMessage request)
    {
      IEnumerable<string> values;

      string customHeaderValue = "Unknown";
      if (request != null)
      {
        if (request.Headers != null)
        {
          request.Headers.TryGetValues("Authorization", out values);

          if (values != null)
          {
            customHeaderValue = values.ToArray()[0];
            if (string.IsNullOrEmpty(customHeaderValue))
            {
              customHeaderValue = "Unknown";
            }
          }
        }
      }

      var value = customHeaderValue.FormatLogContent();
      return value.ToString();
    }

    private static string HideRequestPassword(string requestContent)
    {
      requestContent = System.Uri.UnescapeDataString(requestContent);
      int passindexEqualSign = requestContent.IndexOf("password=");
      string aster = "";

      string[] labelandPass;
      if (passindexEqualSign == 0)
      {
        string extractPass = requestContent.Substring(passindexEqualSign, requestContent.IndexOf("&") - passindexEqualSign);
        labelandPass = extractPass.Split('=');
        for (int x = 0; x < labelandPass[1].Length; x++)
        {
          aster += '*';
        }
        requestContent = requestContent.Replace(labelandPass[1], aster);
      }

      if (passindexEqualSign != 0)
      {
        var stringSplit = requestContent.Split('&');

        for (int i = 0; i < stringSplit.Length; i++)
        {
          if (stringSplit[i].Contains("password="))
          {
            labelandPass = stringSplit[i].Split('=');
            for (int x = 0; x < labelandPass[1].Length; x++)
            {
              aster += '*';
            }
            requestContent = requestContent.Replace(labelandPass[1], aster);
          }
        }

      }

      return requestContent;
    }

    private async Task<string> FormatRequest(HttpRequest request)
    {
      request.EnableRewind();
      var body = request.Body;

      var buffer = new byte[Convert.ToInt32(request.ContentLength)];

      await request.Body.ReadAsync(buffer, 0, buffer.Length);

      var bodyAsText = Encoding.UTF8.GetString(buffer);

      body.Seek(0, SeekOrigin.Begin);

      request.Body = body;

      return $"{bodyAsText}";
    }

    private async Task<string> FormatResponse(HttpResponse response)
    {
      response.Body.Seek(0, SeekOrigin.Begin);

      string responseContent = await new StreamReader(response.Body).ReadToEndAsync();

      response.Body.Seek(0, SeekOrigin.Begin);

      return $"{responseContent}";
    }


    private static string Base64Decode(string base64EncodedData)
    {
      var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
      return Encoding.UTF8.GetString(base64EncodedBytes);
    }

    private static string ResolveReasonPhrase(int statusCode)
    {
      if (statusCode == 200 || statusCode == 201 || statusCode == 204)
      {
        return "OK";
      }
      if (statusCode == 302)
      {
        return "Found";
      }
      if (statusCode == 400)
      {
        return "Bad Request";
      }
      if (statusCode == 401)
      {
        return "Unauthorized";
      }
      if (statusCode == 403)
      {
        return "Forbidden";
      }
      if (statusCode == 404)
      {
        return "Not Found";
      }
      if (statusCode == 409)
      {
        return "Conflict";
      }

      if (statusCode >= 500)
      {
        return "Error";
      }
      return string.Empty;
    }

    private static List<string> GetDisallowedRequests()
    {
      List<string> extensions = new List<string>() { ".js", ".ico", ".svg", ".woff2", ".css", ".png", ".jpg", ".eot", ".otf", ".ttf", ".woff","swagger" };

      return extensions;
    }
  }



  /// <summary>
  /// Format the log content to remove commas.
  /// </summary>
  public static class LogFormatter
  {
    /// <summary>
    /// remove commmas for request/response content.
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static string FormatLogContent(this string content)
    {
      StringBuilder value = new StringBuilder();

      if (content == null)
      {
        content = "";
      }

      value.Append(content);

      if (content.Contains(","))
      {
        value.Replace(",", ";");
      }

      return value.ToString();
    }

  }

}
