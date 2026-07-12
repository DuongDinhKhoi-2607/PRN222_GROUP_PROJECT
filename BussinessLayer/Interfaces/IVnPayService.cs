using BussinessLayer.DTOs.VNPay;
using Microsoft.AspNetCore.Http;

namespace BussinessLayer.Interfaces
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
