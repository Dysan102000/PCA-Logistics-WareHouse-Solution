using Microsoft.AspNetCore.Components;
using System.Text.Json;
using PcaLogWarehouse.Shared;

// Note: Ensure the namespace matches your project structure
namespace PCA_Logistics_WareHouse_Web_App.Components.Pages;

// The 'partial' keyword is essential to link this class to the .razor file.
public partial class Shipments : ComponentBase
{
    // --- Properties ---
    public List<GrnModel>? AllShipments { get; set; }
    public List<GrnModel>? FilteredShipments { get; set; }

    // Backing field for search functionality
    private string _searchTerm = string.Empty;

    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            _searchTerm = value;
            FilterShipments();
        }
    }

    // --- Lifecycle Methods ---

    protected override async Task OnInitializedAsync()
    {
        // Load initial data when the page starts
        LoadShipmentData();
        await Task.CompletedTask;
    }

    // --- Core Methods ---

    public void LoadShipmentData()
    {
        // To reuse the data loading logic, we instantiate a temporary GrnScanning object
        // This is a quick-and-dirty way to access the LoadAllGrnRecords method.
        // In a real application, this logic would be moved to a shared 'DataService'.
        var grnScanner = new GrnScanning();
        AllShipments = grnScanner.LoadAllGrnRecords();

        // Set the filtered list to the full list initially
        FilteredShipments = AllShipments;

        // Re-apply filter in case SearchTerm was set before data loaded
        FilterShipments();
    }

    public void FilterShipments()
    {
        if (AllShipments == null)
        {
            FilteredShipments = new List<GrnModel>();
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            FilteredShipments = AllShipments;
        }
        else
        {
            string searchLower = SearchTerm.ToLowerInvariant();
            FilteredShipments = AllShipments
                .Where(s =>
                    s.ReceiptNo.ToLowerInvariant().Contains(searchLower) ||
                    s.DriverName.ToLowerInvariant().Contains(searchLower)
                )
                .ToList();
        }
    }

    // --- Re-usable Data Model Class (If GrnModel is not in a shared Data folder) ---
    // Since your other files use 'PCA_Logistics_WareHouse_Web_App.Data', 
    // we assume GrnModel is globally available. If not, include the model here.
}