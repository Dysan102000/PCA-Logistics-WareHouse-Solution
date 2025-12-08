using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PCA_Logistics_WareHouse_Web_App.Data;
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Components.Forms; 
using Microsoft.AspNetCore.Hosting;

// You may need to adjust this namespace based on your project structure.
namespace PCA_Logistics_WareHouse_Web_App.Components.Pages
{
    // The 'partial' keyword is essential to link this class to the .razor file.
    public partial class GrnScanning : ComponentBase
    {
        // Dependency Injection
        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        // NEW: Injected to handle page navigation
        public NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        // NEW: Injected to get the server path for saving files (e.g., wwwroot)
        public IWebHostEnvironment WebHostEnvironment { get; set; } = default!;

        // Fields
        protected ElementReference inputElement;
        // NOTE: Assuming GrnModel has the List<string> ScannedDocumentReferences property added previously.
        protected GrnModel grnModel = new GrnModel();

        // Internal fields
        private string grnIdInput = string.Empty;
        private string grnReceivedFrom = string.Empty;
        private bool isFirstRender = true;
        private GrnProductLine grnProdLine = new();

        private const int MaxFileSize = 10 * 1024 * 1024; // 10MB limit (NEW)


        // Constructor
        public GrnScanning()
        {
            grnModel.ProductLines = new();
            // Assuming ArrivalTimestamp is DateTime? now, as per the previous fix.
        }

        // Blazor Lifecycle Methods
        protected override void OnInitialized()
        {
            // Check if the timestamp is null (i.e., this is a brand new GRN being created)
            if (grnModel.ArrivalTimestamp == null)
            {
                grnModel.ArrivalTimestamp = DateTime.Now;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && isFirstRender)
            {
                await JSRuntime.InvokeVoidAsync("focusElement", inputElement);
                isFirstRender = false;
            }
        }

        // =========================================================
        // UI Logic and Methods
        // =========================================================

        // NEW METHOD: File Upload Handling
        public async Task HandleFileSelection(InputFileChangeEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(grnModel.ReceiptNo))
            {
                await JSRuntime.InvokeVoidAsync("alert", "Please enter a Receipt No. before uploading documents.");
                return;
            }

            // Define a path to save the files: wwwroot/uploads
            var uploadsPath = Path.Combine(WebHostEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath); // Ensure the directory exists

            // Initialize list if null
            if (grnModel.ScannedDocumentReferences == null)
            {
                grnModel.ScannedDocumentReferences = new List<string>();
            }

            foreach (var file in e.GetMultipleFiles())
            {
                try
                {
                    // Validation
                    if (file.Size > MaxFileSize)
                    {
                        await JSRuntime.InvokeVoidAsync("alert", $"File '{file.Name}' exceeds the 10MB limit.");
                        continue;
                    }

                    // Create a unique file name linked to the GRN
                    var fileExtension = Path.GetExtension(file.Name);
                    var newFileName = $"{grnModel.ReceiptNo}_{Guid.NewGuid()}{fileExtension}";
                    var fullPath = Path.Combine(uploadsPath, newFileName);

                    // Copy the file stream to the disk
                    using (var stream = file.OpenReadStream(MaxFileSize))
                    using (var fs = new FileStream(fullPath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fs);
                    }

                    // Add the file reference (only the name) to the model
                    grnModel.ScannedDocumentReferences.Add(newFileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"File upload error: {ex.Message}");
                    await JSRuntime.InvokeVoidAsync("alert", $"Error uploading file: {file.Name}. Check server logs.");
                }
            }
        }

        public void DevHydrateFields()
        {
            #region SampleDev Input
            GrnModel hydrateModel = new();
            hydrateModel.ReceiptNo = "16243";
            hydrateModel.ReceivedFrom = "Schoonbee";
            hydrateModel.ReceivedFor = "GREEN SOUTH";
            hydrateModel.BoxCount = 625;
            hydrateModel.TotalWeightKg = 4122;
            hydrateModel.DriverName = "Musa Sokhela";
            hydrateModel.DriverIdNo = "8208286417085";
            hydrateModel.TruckRegNo = "BM 31 GKZN";
            hydrateModel.ArrivalTimestamp = DateTime.Now;
            hydrateModel.ProductLines = new();
            GrnProductLine productLine = new();
            productLine.Product = "Grapes";
            productLine.DeliveryNoteNo = "106703";
            productLine.QuantityOnDeliveryNote = 5;
            productLine.QuantityReceived = 5;
            productLine.Temperature = 9.7m;
            hydrateModel.IsDriversIdentityChecked = true;
            hydrateModel.IsVehicleChecked = true;
            hydrateModel.AreDeliveryDocumentsChecked = true;
            hydrateModel.IsKnownConsignorChecked = true;
            hydrateModel.IsCargoStoredSecurely = true;
            hydrateModel.IsDangerousGoods = false;
            // NEW: Initialize document list
            hydrateModel.ScannedDocumentReferences = new List<string>();

            hydrateModel.ProductLines.Add(productLine);
            #endregion
            grnModel = hydrateModel;
        }

        public void LoadGrnDetails()
        {
            // --- SIMULATED DATA LOAD (using data from your PDF) ---
            grnModel = new GrnModel
            {
                ReceiptNo = grnModel.ReceiptNo.Trim(),
                ReceivedFrom = "Schoonbee",
                ReceivedFor = "GREEN SOUTH",
                BoxCount = 625,
                TotalWeightKg = 4122,
                ArrivalTimestamp = new DateTime(2025, 11, 25, 15, 18, 0),
                DriverName = "Musa Sokhela",
                DriverIdNo = "8208286417085",
                TruckRegNo = "BM 31 GKZN",

                // Simulating a loaded product line
                ProductLines = new List<GrnProductLine>
                {
                    new GrnProductLine
                    {
                        Product = "GRAPES",
                        DeliveryNoteNo = "106203 05",
                        QuantityOnDeliveryNote = 5,
                        QuantityReceived = 5,
                        Dims = "HIGH CUBE PALLET",
                        Temperature = 9.7M
                    }
                },
                // NEW: Initialize document list
                ScannedDocumentReferences = new List<string>(),

                // Simulating the AVSEC checks as they appear on the document
                IsDriversIdentityChecked = true,
                IsVehicleChecked = true,
                AreDeliveryDocumentsChecked = true,
                IsKnownConsignorChecked = true,
                IsCargoStoredSecurely = true,
                IsDangerousGoods = false,
            };
            // --- END SIMULATED DATA LOAD ---
        }

        public void HandleGrnIdInput(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                ValidateGrnDetails();
            }
        }

        public void ValidateGrnDetails()
        {
            LoadGrnDetails();
        }

        public void FinalizeGrnProcessing()
        {
            bool allRequiredChecked = grnModel.IsDriversIdentityChecked
                && grnModel.IsVehicleChecked && grnModel.AreDeliveryDocumentsChecked
                && grnModel.IsKnownConsignorChecked && grnModel.IsCargoStoredSecurely;

            if (!allRequiredChecked)
            {
                Console.WriteLine("ERROR: Not all AVSEC security checks have been confirmed.");
                return;
            }

            // Check if required documents are uploaded (e.g., at least one document)
            if (grnModel.ScannedDocumentReferences == null || !grnModel.ScannedDocumentReferences.Any())
            {
                Console.WriteLine("ERROR: No scanned documents were uploaded.");
                // You might want to return here or show a warning to the user
            }

            SaveGrnIfNotExist(grnModel);
            Console.WriteLine($"GRN {grnModel.ReceiptNo} finalized and saved successfully!");

            // Reset UI for next scan
            grnIdInput = string.Empty;
            grnModel = new GrnModel { ArrivalTimestamp = DateTime.Now };
        }

        // ADDED: Re-adding the stub for the missing SaveGrnData method to fix the previous build error, 
        // as it seems to be used as a general save button.
        public void SaveGrnData()
        {
            FinalizeGrnProcessing();
        }


        public void SaveGrnIfNotExist(GrnModel pGrnToSave)
        {
            const string filePath = "GrnRecords.json";
            try
            {
                string jsonString = JsonSerializer.Serialize(pGrnToSave);
                File.AppendAllText(filePath, jsonString + Environment.NewLine);

                Console.WriteLine($"Successfully saved GRN {pGrnToSave.ReceiptNo} to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR saving GRN {pGrnToSave.ReceiptNo}: {ex.Message}");
            }
        }

        // Method to add a new empty line to the list
        public void AddProductLine()
        {
            if (grnModel.ProductLines == null)
            {
                grnModel.ProductLines = new List<GrnProductLine>();
            }

            grnModel.ProductLines.Add(new GrnProductLine());
        }

        // Method to remove a specific line
        public void RemoveProductLine(GrnProductLine line)
        {
            grnModel.ProductLines.Remove(line);
        }

        // Method to load all GRNs from the file (needed for lookup)
        public List<GrnModel> LoadAllGrnRecords()
        {
            const string filePath = "GrnRecords.json";
            List<GrnModel> recordsToReturn = new List<GrnModel>();

            if (File.Exists(filePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(filePath);
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            GrnModel? grn = JsonSerializer.Deserialize<GrnModel>(line);
                            if (grn != null)
                            {
                                recordsToReturn.Add(grn);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR loading GRN records: {ex.Message}");
                }
            }
            return recordsToReturn;
        }

        public async Task ViewGrnPdf()
        {
            if (string.IsNullOrWhiteSpace(grnModel.ReceiptNo))
            {
                await JSRuntime.InvokeVoidAsync("alert", "Please enter a Receipt No. before viewing the GRN.");
                return;
            }

            // 1. Define the URL for the new page, passing the ReceiptNo as a parameter
            // This is the URL of the new component we are creating: /viewgrn/{receiptno}
            var url = $"/viewgrn/{grnModel.ReceiptNo}";

            // 2. Use JSRuntime to open the URL in a new browser tab/window
            // The '_blank' target ensures a new tab is opened.
            await JSRuntime.InvokeVoidAsync("open", url, "_blank");

            // NOTE: The actual PDF generation will need to be triggered by the 
            // ViewGrn.razor component using the ReceiptNo.
        }
    }
}