using __PROJECT_NAME__.Domain.DTOs.Share;

namespace __PROJECT_NAME__.API.Utilities;

/// <summary>
/// 將可預期的業務例外轉換為一致的 HTTP 錯誤回應。
/// 由 ApiExceptionFilter 與 ApiExceptionHandlingMiddleware 共用，
/// 確保兩條路徑產出的錯誤格式完全相同。
/// </summary>
public static class ApiExceptionResponseUtility
{
    /// <summary>
    /// 未預期例外對外顯示的訊息，避免洩漏系統內部細節
    /// </summary>
    public const string UnexpectedErrorMessage = "系統發生錯誤，請稍後再試";

    /// <summary>
    /// 解析例外對應的 HTTP 狀態碼與對外訊息
    /// </summary>
    /// <param name="exception">攔截到的例外</param>
    /// <returns>狀態碼、對外訊息，以及是否為可預期的業務例外</returns>
    public static (int StatusCode, string Message, bool IsExpected) Resolve(Exception exception) => exception switch
    {
        ArgumentException => (StatusCodes.Status400BadRequest, exception.Message, true),
        KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message, true),
        InvalidOperationException => (StatusCodes.Status409Conflict, exception.Message, true),
        _ => (StatusCodes.Status500InternalServerError, UnexpectedErrorMessage, false)
    };

    /// <summary>
    /// 建立錯誤回應的 Body
    /// </summary>
    /// <param name="exception">攔截到的例外</param>
    /// <returns>統一格式的錯誤回應</returns>
    public static BaseResponse CreateBody(Exception exception)
    {
        var (_, message, _) = Resolve(exception);
        return new BaseResponse { Success = false, Message = message };
    }
}
