using System.Threading.Tasks;

namespace BussinessLayer.Interfaces
{
    public interface IEmailService
    {
        Task SendActivationLinkAsync(string email, string name, string activationLink);
        Task SendLecturerCredentialsAsync(string email, string name, string tempPassword, string loginUrl);
    }
}
