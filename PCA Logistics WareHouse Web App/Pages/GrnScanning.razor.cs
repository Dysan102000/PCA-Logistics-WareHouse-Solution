using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.JSInterop;
using PCA_Logistics_WareHouse_Web_App.Data;
using PCA_Logistics_WareHouse_Web_App.Shared;
// QuestPDF USINGS
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Linq;
using System.Text.Json;

// You may need to adjust this namespace based on your project structure.
namespace PCA_Logistics_WareHouse_Web_App.Components.Pages
{
    public partial class GrnScanning : ComponentBase
    {
        // Dependency Injection
        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        public IWebHostEnvironment WebHostEnvironment { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        // Fields
        protected ElementReference inputElement;
        protected GrnModel grnModel = new GrnModel();

        // Internal fields
        private string grnIdInput = string.Empty;
        private string grnReceivedFrom = string.Empty;
        private bool isFirstRender = true;
        private GrnProductLine grnProdLine = new();
        private byte[]? pcaLogoBytes;
        private const string LogoPath = "img/pca_logo_5.png";

        private const int MaxFileSize = 10 * 1024 * 1024; // 10MB limit

        // =========================================================
        // NOTIFICATION METHODS
        // =========================================================
        // 1. ADD FIELD TO REFERENCE THE COMPONENT
        protected AppNotification? appNotification;

        // 2. Add methods to call the component's ShowNotification method
        private void ShowSuccess(string title, string message) =>
            appNotification?.ShowNotification(title, message, "success");

        private void ShowError(string title, string message) =>
            appNotification?.ShowNotification(title, message, "danger", 8000); // Longer duration for errors

        // Constructor
        public GrnScanning()
        {
            grnModel.ProductLines = new();
            // Ensure ProductLines and ScannedDocumentReferences are initialized on creation
            grnModel.ScannedDocumentReferences = new List<string>();
        }

        // Blazor Lifecycle Methods
        protected override void OnInitialized()
        {
            // 🚩 FIX: Set the QuestPDF license type here.
            QuestPDF.Settings.License = LicenseType.Community;

            // 🚩 NEW: Load the company logo into a byte array
            var fullPath = Path.Combine(WebHostEnvironment.WebRootPath, LogoPath);
            if (File.Exists(fullPath))
            {
                try
                {
                    pcaLogoBytes = File.ReadAllBytes(fullPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR loading company logo: {ex.Message}");
                    // Keep pcaLogoBytes as null, the fallback text will be used.
                }
            }
            else
            {
                Console.WriteLine($"WARNING: Company logo file not found at: {fullPath}");
            }

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
        // NEW METHOD: Handle Camera Photos
        // =========================================================
        public async Task HandlePhotoSelection(InputFileChangeEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(grnModel.ReceiptNo))
            {
                await JSRuntime.InvokeVoidAsync("alert", "Please enter a Receipt No. before taking photos.");
                return;
            }

            // Reuse the same uploads folder
            var uploadsPath = Path.Combine(WebHostEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            if (grnModel.ScannedDocumentReferences == null)
            {
                grnModel.ScannedDocumentReferences = new List<string>();
            }

            foreach (var file in e.GetMultipleFiles())
            {
                try
                {
                    if (file.Size > MaxFileSize)
                    {
                        await JSRuntime.InvokeVoidAsync("alert", $"Image '{file.Name}' exceeds the 10MB limit.");
                        continue;
                    }

                    var fileExtension = Path.GetExtension(file.Name);
                    // 🚩 FORCE EXTENSION: Sometimes mobile cameras don't send extensions correctly, 
                    // defaulting to .jpg is usually safe for photos, or just trust the file name.
                    if (string.IsNullOrEmpty(fileExtension)) fileExtension = ".jpg";

                    // 🚩 NAMING: Prefix with IMG_ to distinguish from docs
                    var newFileName = $"IMG_{grnModel.ReceiptNo}_{Guid.NewGuid()}{fileExtension}";
                    var fullPath = Path.Combine(uploadsPath, newFileName);

                    using (var stream = file.OpenReadStream(MaxFileSize))
                    using (var fs = new FileStream(fullPath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fs);
                    }

                    grnModel.ScannedDocumentReferences.Add(newFileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Photo upload error: {ex.Message}");
                    await JSRuntime.InvokeVoidAsync("alert", "Error saving photo. Please try again.");
                }
            }
        }

        // =========================================================
        // UI Logic and Methods
        // =========================================================
        public async Task HandleFileSelection(InputFileChangeEventArgs e)
        {
            // ... (HandleFileSelection method content remains the same) ...
            if (string.IsNullOrWhiteSpace(grnModel.ReceiptNo))
            {
                await JSRuntime.InvokeVoidAsync("alert", "Please enter a Receipt No. before uploading documents.");
                return;
            }

            var uploadsPath = Path.Combine(WebHostEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            if (grnModel.ScannedDocumentReferences == null)
            {
                grnModel.ScannedDocumentReferences = new List<string>();
            }

            foreach (var file in e.GetMultipleFiles())
            {
                try
                {
                    if (file.Size > MaxFileSize)
                    {
                        await JSRuntime.InvokeVoidAsync("alert", $"File '{file.Name}' exceeds the 10MB limit.");
                        continue;
                    }

                    var fileExtension = Path.GetExtension(file.Name);
                    var newFileName = $"{grnModel.ReceiptNo}_{Guid.NewGuid()}{fileExtension}";
                    var fullPath = Path.Combine(uploadsPath, newFileName);

                    using (var stream = file.OpenReadStream(MaxFileSize))
                    using (var fs = new FileStream(fullPath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fs);
                    }

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
            hydrateModel.ScannedDocumentReferences = new List<string>();

            hydrateModel.ProductLines.Add(productLine);
            #endregion
            grnModel = hydrateModel;

            ShowSuccess("Autofill", "Fields filled.");
        }

        public void LoadGrnDetails()
        {
            // ... (LoadGrnDetails method content remains the same) ...
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
                ScannedDocumentReferences = new List<string>(),

                IsDriversIdentityChecked = true,
                IsVehicleChecked = true,
                AreDeliveryDocumentsChecked = true,
                IsKnownConsignorChecked = true,
                IsCargoStoredSecurely = true,
                IsDangerousGoods = false,
            };
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
                //Console.WriteLine("ERROR: Not all AVSEC security checks have been confirmed.");
                ShowError("GRN NOT Finalized!", "ERROR: Not all AVSEC security checks have been confirmed.");
                return;
            }

            if (grnModel.ScannedDocumentReferences == null || !grnModel.ScannedDocumentReferences.Any())
            {
                //Console.WriteLine("ERROR: No scanned documents were uploaded.");
                ShowError("Documents!", "ERROR: No scanned documents were uploaded.");

            }

            // ACTION: Save the GRN data
            SaveGrnIfNotExist(grnModel);
            //ShowSuccess("GRN Finalized!", $"Receipt No. {grnModel.ReceiptNo} has been successfully saved and finalized.", 6000);
            ShowSuccess("GRN Finalized!", $"Receipt No. {grnModel.ReceiptNo} has been successfully saved and finalized.");
            //Console.WriteLine($"GRN {grnModel.ReceiptNo} finalized and saved successfully!");

            // CRUCIAL CHANGE: REMOVED THE PAGE RESET HERE.
        }

        // NEW METHOD: Dedicated method to reset the page for a new GRN
        public void ResetForNewGrn()
        {
            // Reset UI for next scan
            grnIdInput = string.Empty;
            grnModel = new GrnModel { ArrivalTimestamp = DateTime.Now, ProductLines = new List<GrnProductLine>(), ScannedDocumentReferences = new List<string>() };
            StateHasChanged(); // Force a UI refresh
        }


        // SaveGrnData is the main method called by the two finalize buttons
        public void SaveGrnData()
        {
            FinalizeGrnProcessing();
        }

        // ... (SaveGrnIfNotExist, AddProductLine, RemoveProductLine, LoadAllGrnRecords remain the same) ...

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

        public void AddProductLine()
        {
            if (grnModel.ProductLines == null)
            {
                grnModel.ProductLines = new List<GrnProductLine>();
            }

            grnModel.ProductLines.Add(new GrnProductLine());
        }

        public void RemoveProductLine(GrnProductLine line)
        {
            grnModel.ProductLines.Remove(line);
        }

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


        // =========================================================
        // PDF GENERATION & VIEW METHOD
        // =========================================================

        public async Task ViewGrnPdf()
        {
            string receiptNoToFind = grnModel.ReceiptNo;

            if (string.IsNullOrWhiteSpace(receiptNoToFind))
            {
                await JSRuntime.InvokeVoidAsync("alert", "Please enter a Receipt No. before viewing the GRN.");
                return;
            }

            // 1. Attempt to find the saved GRN data in the file
            var foundGrn = LoadAllGrnRecords()
                               .FirstOrDefault(g => string.Equals(g.ReceiptNo, receiptNoToFind, StringComparison.OrdinalIgnoreCase));

            // 2. Fallback: If not found in file, use the current page model (grnModel).
            // This ensures the PDF works immediately after saving before a page refresh.
            if (foundGrn == null)
            {
                if (string.Equals(grnModel.ReceiptNo, receiptNoToFind, StringComparison.OrdinalIgnoreCase) && grnModel.ProductLines.Any())
                {
                    // Use the current model's data
                    foundGrn = grnModel;
                    Console.WriteLine($"WARNING: GRN '{receiptNoToFind}' not found in file, using current in-memory model data.");
                }
                else
                {
                    // If no saved record AND no current model data, show the alert.
                    await JSRuntime.InvokeVoidAsync("alert", $"GRN with Receipt No. '{receiptNoToFind}' not found in saved records. Please save it first.");
                    return;
                }
            }

            // 3. Generate the PDF bytes
            var pdfBytes = GenerateGrnPdfBytes(foundGrn);
            var base64String = Convert.ToBase64String(pdfBytes);

            // 🚩 FIX: Use the new JavaScript function to open the PDF as a Data URI in a new tab.
            // The filename parameter is no longer strictly necessary but can be kept if needed.
            await JSRuntime.InvokeVoidAsync("openPdfInNewTab", base64String);
        }

        private byte[] GenerateGrnPdfBytes(GrnModel model)
        {
            // The logic from the QuestPDF DocumentGenerator is embedded here.
            string pcaPrimaryColor = "#183B7D";
            string pcaAccentColor = "#AEC435";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);

                    // HEADER
                    page.Header().Row(row =>
                    {
                        // 🚩 Left Column (RelativeItem 1): Logo followed by Title
                        row.RelativeItem(1).Column(stack =>
                        {
                            // 1. LOGO/FALLBACK LOGIC
                            if (pcaLogoBytes != null && pcaLogoBytes.Length > 0)
                            {
                                try
                                {
                                    // Logo is left-aligned by default inside the Column
                                    stack.Item().Height(50).Width(170).Image(pcaLogoBytes).FitArea();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"QuestPDF Image Error: Failed to process logo image. {ex.Message}");
                                    stack.Item().Text("PCA LOGISTICS - Image Failed").FontSize(18).Bold().FontColor(pcaPrimaryColor);
                                }
                            }
                            else
                            {
                                // Fallback text
                                stack.Item().Text("PCA LOGISTICS").FontSize(18).Bold().FontColor(pcaPrimaryColor);
                            }

                            // 2. TITLE TEXT (Placed directly below the logo/fallback)
                            stack.Item().PaddingTop(5).Text("Goods Received Note (GRN)").FontSize(14).FontColor(pcaAccentColor);
                        });

                        // Right Column (RelativeItem 2): GRN NO and Date
                        row.RelativeItem(1).AlignRight().Column(stack =>
                        {
                            stack.Item().Text($"GRN NO: {model.ReceiptNo}").FontSize(18).SemiBold().FontColor(pcaPrimaryColor).Underline();

                            var displayDate = model.ArrivalTimestamp?.ToString("yyyy-MM-dd HH:mm") ?? "N/A";
                            stack.Item().Text($"Date: {displayDate}").FontSize(10);
                        });
                    });

                    // CONTENT
                    page.Content().Column(stack =>
                    {
                        // Client Details
                        stack.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(10).Column(column =>
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem(1).Column(s => { s.Item().Text("Received From:").FontSize(8).FontColor(Colors.Grey.Darken1); s.Item().Text(model.ReceivedFrom).SemiBold().FontSize(10); });
                                row.RelativeItem(1).Column(s => { s.Item().Text("Received For:").FontSize(8).FontColor(Colors.Grey.Darken1); s.Item().Text(model.ReceivedFor).SemiBold().FontSize(10); });
                            });
                        });

                        // Shipment Details
                        stack.Item().PaddingTop(15).Background(Colors.Grey.Lighten5).Padding(10).Column(column =>
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem(1).Column(s => { s.Item().Text("Driver Name:").FontSize(8).FontColor(Colors.Grey.Darken1); s.Item().Text(model.DriverName).SemiBold().FontSize(10); });
                                row.RelativeItem(1).Column(s => { s.Item().Text("Truck Reg. No.:").FontSize(8).FontColor(Colors.Grey.Darken1); s.Item().Text(model.TruckRegNo).SemiBold().FontSize(10); });
                            });
                        });

                        // Product Table Title
                        stack.Item().PaddingVertical(20).Text("Product Lines").SemiBold().FontSize(12);

                        // Product Table
                        stack.Item().Table(table =>
                        {
                            // Columns
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); columns.RelativeColumn(1); columns.RelativeColumn(1); columns.RelativeColumn(1); columns.RelativeColumn(1);
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Padding(5).Text("Product").SemiBold().FontColor(Colors.White).BackgroundColor(pcaAccentColor);
                                header.Cell().Padding(5).Text("Del. Note No.").SemiBold().FontColor(Colors.White).BackgroundColor(pcaAccentColor);
                                header.Cell().Padding(5).Text("Qty DN").SemiBold().FontColor(Colors.White).BackgroundColor(pcaAccentColor);
                                header.Cell().Padding(5).Text("Qty Rec.").SemiBold().FontColor(Colors.White).BackgroundColor(pcaAccentColor);
                                header.Cell().Padding(5).Text("Temp (°C)").SemiBold().FontColor(Colors.White).BackgroundColor(pcaAccentColor);
                                header.Cell().ColumnSpan(5).Padding(1).Background(Colors.Black).Text(string.Empty);
                            });

                            // Rows
                            foreach (var item in model.ProductLines)
                            {
                                table.Cell().Padding(5).BorderBottom(0.5f).Text(item.Product);
                                table.Cell().Padding(5).BorderBottom(0.5f).Text(item.DeliveryNoteNo);
                                table.Cell().Padding(5).BorderBottom(0.5f).AlignRight().Text(item.QuantityOnDeliveryNote.ToString());
                                table.Cell().Padding(5).BorderBottom(0.5f).AlignRight().Text(item.QuantityReceived.ToString());
                                table.Cell().Padding(5).BorderBottom(0.5f).AlignRight().Text($"{item.Temperature:F1}");
                            }
                        });

                        // AVSEC Checks Title
                        stack.Item().PaddingVertical(20).Text("AVSEC Security Checklist").SemiBold().FontSize(12);

                        // AVSEC Checks
                        stack.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns => { columns.ConstantColumn(80); columns.RelativeColumn(5); });

                            void CheckRow(string label, bool status)
                            {
                                table.Cell().Padding(5).Text(status ? "✅ PASS" : "❌ FAIL").SemiBold().FontColor(status ? Colors.Green.Medium : Colors.Red.Medium);
                                table.Cell().Padding(5).Text(label);
                            }

                            CheckRow("Driver ID Checked.", model.IsDriversIdentityChecked);
                            CheckRow("Vehicle Checked for tampering.", model.IsVehicleChecked);
                            CheckRow("Delivery Documents Checked.", model.AreDeliveryDocumentsChecked);
                            CheckRow("Known Consignor Status Confirmed.", model.IsKnownConsignorChecked);
                            CheckRow("Cargo Stored Securely.", model.IsCargoStoredSecurely);
                            CheckRow("Dangerous Goods Declaration.", model.IsDangerousGoods);
                        });

                        stack.Item().PaddingTop(20).Text($"GRN Finalized By: [Warehouse Staff Name/Signature]")
                            .SemiBold().FontSize(10);
                    });

                    // FOOTER
                    page.Footer().Text(x => { x.Span("Page ").FontSize(8); x.CurrentPageNumber().FontSize(8); x.Span(" of ").FontSize(8); x.TotalPages().FontSize(8); x.AlignRight(); });
                });
            });

            return document.GeneratePdf();
        }

    }
}