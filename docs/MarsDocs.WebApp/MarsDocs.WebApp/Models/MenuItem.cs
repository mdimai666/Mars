namespace MarsDocs.WebApp.Models;

public record MenuItem(string Path,
                        string Url,
                        string FileName,
                        string Title,
                        bool IsDivider = false,
                        bool SubItemFlag = false,
                        MenuItem[] SubItems = default!,
                        float Order = 10f)
{
    public float Order = Order;

    public void SetOrder(float order)
    {
        Order = order;
    }
};
