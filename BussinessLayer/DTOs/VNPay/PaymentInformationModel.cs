namespace BussinessLayer.DTOs.VNPay
{
    public class PaymentInformationModel
    {
        public int OrderID { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string OrderDescription { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
