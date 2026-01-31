using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace PCA_Logistics_WareHouse_Web_App.Components.Shared
{

    public partial class CompanySearchInput : ComponentBase
    {
        [Parameter] public string InputId { get; set; } = Guid.NewGuid().ToString("N");
        [Parameter] public string Placeholder { get; set; } = "Start typing to search or enter a new value";
        [Parameter] public List<string> MasterList { get; set; } = new List<string>();
        [Parameter] public string Value { get; set; } = string.Empty;
        [Parameter] public EventCallback<string> ValueChanged { get; set; }
        [Parameter] public EventCallback<string> OnNewCompanyEntered { get; set; }

        protected List<string> FilteredList { get; set; } = new List<string>();
        protected bool ShowDropdown { get; set; } = false;
        private bool _dropdownClickPrevented = false;

        protected override void OnInitialized() => PerformFiltering();



        // Called when the user types
        protected async Task OnValueChange(string newValue)
        {
            await ValueChanged.InvokeAsync(newValue);
            Value = newValue;

            PerformFiltering();
            ShowDropdown = true;
        }

        private void PerformFiltering()
        {
            var searchTerm = Value.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                FilteredList = MasterList.Take(10).ToList(); // Show top 10 initially
            }
            else
            {
                FilteredList = MasterList
                    .Where(c => c.ToLowerInvariant().Contains(searchTerm))
                    .OrderBy(c => c)
                    .Take(20)
                    .ToList();
            }
        }

        protected async Task SelectCompany(string company)
        {
            await OnValueChange(company);
            ShowDropdown = false;
            _dropdownClickPrevented = true; // Prevents blur handler from running save/hide logic prematurely
        }

        protected void HandleInputFocus(FocusEventArgs e)
        {
            if (!ShowDropdown)
            {
                PerformFiltering();
                ShowDropdown = true;
            }
        }

        protected async Task HandleInputBlur(FocusEventArgs e)
        {
            // Give time for the SelectCompany click event to fire
            await Task.Delay(200);

            if (_dropdownClickPrevented)
            {
                _dropdownClickPrevented = false; // Reset for next time
                return;
            }

            // --- Logic to Save New Company ---
            var cleanValue = Value.Trim();

            // Check if the value is non-empty and is NOT in the master list
            if (!string.IsNullOrWhiteSpace(cleanValue) &&
                !MasterList.Any(c => c.Equals(cleanValue, StringComparison.OrdinalIgnoreCase)))
            {
                // New company, trigger the save event in the parent (GrnScanning)
                await OnNewCompanyEntered.InvokeAsync(cleanValue);
            }

            // Hide the dropdown
            ShowDropdown = false;
        }

        // --- MISSING METHOD ADDED HERE ---
        protected async Task HandleKeyDown(KeyboardEventArgs e)
        {
            // If the user hits Enter and there is a match in the list, select the top one.
            if (e.Key == "Enter" && ShowDropdown && FilteredList.Any())
            {
                var bestMatch = FilteredList.First();
                await SelectCompany(bestMatch);
            }
        }
    }
}