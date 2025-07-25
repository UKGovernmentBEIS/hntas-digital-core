namespace HNTAS.Core.Api.Interfaces
{
    public interface IGovUkNotifyService
    {
        /// <summary>
        /// Sends an email using the GOV.UK Notify API.
        /// </summary>
        /// <param name="emailAddress">The recipient's email address.</param>
        /// <param name="templateId">The ID of the email template to use.</param>
        /// <param name="personalisation">A dictionary of personalization fields for the template.</param>
        /// <param name="reference">An optional unique reference for this notification.</param>
        /// <returns>True if the email was sent successfully, false otherwise.</returns>
        Task<bool> SendEmailAsync(
            string emailAddress,
            string templateId,
            Dictionary<string, dynamic>? personalisation = null,
            string? reference = null);
    }
}
