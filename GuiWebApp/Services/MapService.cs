using Microsoft.JSInterop;

namespace GuiWebApp.Client.Services
{
    public class MapService
    {

        private IJSRuntime jSRuntime { get; set; }

        public MapService(IJSRuntime jSRuntime)
        {
            this.jSRuntime = jSRuntime;
        }

        public async Task CreateMap(double startingLat, double startingLng, double startingZoom)
        {
            await jSRuntime.InvokeVoidAsync("createMap", startingLat, startingLng, startingZoom);
        }

        public async Task AddPoint(double lat, double lng)
        {
            await jSRuntime.InvokeVoidAsync("addPoint", lat, lng);
        }

        public async Task CreatePath(List<List<double>> coordinates, String color)
        {
            await jSRuntime.InvokeVoidAsync("createPath", coordinates, color);
        }
    }
}
