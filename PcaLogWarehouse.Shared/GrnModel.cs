namespace PcaLogWarehouse.Shared;

public class GrnModel
{
    // Header Information (from PCA Logistics GRN)
    public string ReceiptNo { get; set; } = string.Empty; // e.g., 16243
    public string ReceivedFrom { get; set; } = string.Empty; // e.g., Schoonbee
    public string ReceivedFor { get; set; } = string.Empty; // e.g., GREEN SOUTH
    public int BoxCount { get; set; } // e.g., 625
    public decimal TotalWeightKg { get; set; } // e.g., 4122KG
    public DateTime? ArrivalTimestamp { get; set; } // e.g., 20-11-25 15:18

    // Driver/Security Details (from AVSEC/Driver Section)
    public string DriverName { get; set; } = string.Empty;
    public string DriverIdNo { get; set; } = string.Empty;
    public string TruckRegNo { get; set; } = string.Empty;

    // Cargo Details
    public List<GrnProductLine> ProductLines { get; set; } = new List<GrnProductLine>();

    // NEW FIELD: References for uploaded files (PDFs/Scans)
    public List<string> ScannedDocumentReferences { get; set; } = new List<string>();

    // AVSEC Checklist (for data entry/confirmation)
    public bool IsDriversIdentityChecked { get; set; } = false;
    public bool IsVehicleChecked { get; set; } = false;
    public bool AreDeliveryDocumentsChecked { get; set; } = false;
    public bool IsKnownConsignorChecked { get; set; } = false;
    public bool IsCargoStoredSecurely { get; set; } = false;
    public bool IsDangerousGoods { get; set; } = false;
}


