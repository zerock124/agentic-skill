namespace __PROJECT_NAME__.Service.Utilities;

/// <summary>
/// Service 層表達業務錯誤的例外工廠。
/// 回傳的例外型別由 API 層的 ApiExceptionResponseUtility 映射成狀態碼，
/// 因此 Service 一律透過這裡建立例外，不要自行定義錯誤碼或回傳失敗旗標。
/// </summary>
/// <example>
/// <code>
/// var entity = repo.Get(x => x.CategoryCode == code && !x.IsDeleted)
///     ?? throw ServiceExceptionUtility.NotFound("查無此分類");
/// </code>
/// </example>
internal static class ServiceExceptionUtility
{
    /// <summary>
    /// 輸入資料不正確（必填、長度、格式、範圍），對應 HTTP 400
    /// </summary>
    /// <param name="message">給使用者看的錯誤訊息</param>
    /// <returns>要擲出的例外</returns>
    public static ArgumentException Argument(string message)
    {
        return new ArgumentException(message);
    }

    /// <summary>
    /// 查無資料（含已軟刪除的資料），對應 HTTP 404
    /// </summary>
    /// <param name="message">給使用者看的錯誤訊息</param>
    /// <returns>要擲出的例外</returns>
    public static KeyNotFoundException NotFound(string message)
    {
        return new KeyNotFoundException(message);
    }

    /// <summary>
    /// 資料重複或目前狀態不允許此操作，對應 HTTP 409
    /// </summary>
    /// <param name="message">給使用者看的錯誤訊息</param>
    /// <returns>要擲出的例外</returns>
    public static InvalidOperationException InvalidOperation(string message)
    {
        return new InvalidOperationException(message);
    }
}
