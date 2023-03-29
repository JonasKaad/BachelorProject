using Microsoft.JSInterop;

namespace GuiWebApp.Client.Services
{
    public class MapService
    {
        private readonly IJSRuntime _jsRuntime;

        public MapService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task CreateMap(double startingLat, double startingLng, double startingZoom)
        {
            await _jsRuntime.InvokeVoidAsync("createMap", startingLat, startingLng, startingZoom);
        }

        public async Task AddPoint(double lat, double lng)
        {
            await _jsRuntime.InvokeVoidAsync("addPoint", lat, lng);
        }

        public async Task CreatePath(List<List<double>> coordinates, string color)
        {
            await _jsRuntime.InvokeVoidAsync("createPath", coordinates, color);
        }

        public async Task ReanimatePath()
        {
            await _jsRuntime.InvokeVoidAsync("reanimatePath");
        }
    }
}
