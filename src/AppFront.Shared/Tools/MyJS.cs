using Microsoft.JSInterop;

namespace AppFront.Shared.Tools
{
    public class MyJS
    {
        private readonly IJSRuntime js;

        public MyJS(IJSRuntime js)
        {
            this.js = js;
        }

        public async ValueTask BeauityJsonInSelector(string selector, string? value = null)
        {
            await js.InvokeVoidAsync("BeauityJsonInSelector", selector, value);
        }

        public void Dispose()
        {
        }

        public async ValueTask OpenNewTab(string url)
        {
            await js.InvokeVoidAsync("blazor_newTab", url);
        }

        public async ValueTask PostMessage(string selector, object data)
        {
            await js.InvokeVoidAsync("BlazorPostMessage", selector, data);
        }

        public async ValueTask CookieRemove(string key)
        {
            await js.InvokeVoidAsync("MyJS_Cookie_Remove", key);
        }
    }
}
