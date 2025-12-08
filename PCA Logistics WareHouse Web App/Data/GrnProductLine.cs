namespace PCA_Logistics_WareHouse_Web_App.Data;

public class GrnProductLine
{
    public string Product { get; set; } = string.Empty; // e.g., GRAPES [cite: 18]
    public string DeliveryNoteNo { get; set; } = string.Empty; // e.g., 106203 05 [cite: 18]
    public int QuantityOnDeliveryNote { get; set; }
    public int QuantityReceived { get; set; } // The value the warehouse user confirms
    public string Dims { get; set; } = string.Empty; // e.g., HIGH CUBE Pallet [cite: 18]
    public decimal Temperature { get; set; } // The value the warehouse user captures, e.g., 9.7 [cite: 18]
}
