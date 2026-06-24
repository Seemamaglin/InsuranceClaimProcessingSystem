using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using InsuranceClaimSystem.Application.Common;
using System.Collections.Generic;

namespace InsuranceClaimSystem.API.Filters;

public class ApiResponseFilterAttribute : ActionFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is EmptyResult || context.Result is NoContentResult)
        {
            base.OnResultExecuting(context);
            return;
        }

        if (context.Result is ObjectResult objectResult)
        {
            var valueType = objectResult.Value?.GetType();
            bool isAlreadyWrapped = valueType != null && valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(ApiResponse<>);

            if (!isAlreadyWrapped && valueType != typeof(ApiResponse))
            {
                if (objectResult.Value is Error error)
                {
                    int statusCode = 400;
                    if (error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase)) statusCode = 404;
                    else if (error.Code.Contains("Conflict", StringComparison.OrdinalIgnoreCase)) statusCode = 409;
                    else if (error.Code.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase)) statusCode = 401;
                    else if (error.Code.Contains("Forbidden", StringComparison.OrdinalIgnoreCase)) statusCode = 403;

                    objectResult.StatusCode = statusCode;
                    objectResult.Value = ApiResponse<object>.Fail(error.Description, new List<Error> { error });
                }
                else
                {
                    var statusCode = objectResult.StatusCode ?? 200;
                    if (statusCode >= 200 && statusCode < 300)
                    {
                        objectResult.Value = ApiResponse<object>.Ok(objectResult.Value!, "Success");
                    }
                    else
                    {
                        objectResult.Value = ApiResponse<object>.Fail("Error", new List<Error> { Error.Validation("HttpError", "Status " + statusCode) });
                    }
                }
            }
        }
        else if (context.Result is StatusCodeResult statusCodeResult)
        {
            if (statusCodeResult.StatusCode >= 200 && statusCodeResult.StatusCode < 300)
            {
                context.Result = new ObjectResult(ApiResponse<object>.Ok(null!, "Success")) { StatusCode = statusCodeResult.StatusCode };
            }
            else
            {
                context.Result = new ObjectResult(ApiResponse<object>.Fail("Error", new List<Error> { Error.Validation("HttpError", "Status " + statusCodeResult.StatusCode) })) { StatusCode = statusCodeResult.StatusCode };
            }
        }

        base.OnResultExecuting(context);
    }
}
