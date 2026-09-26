using Mars.Nodes.Core.Nodes.Common;

namespace Mars.Docker.Front.Nodes.Forms;

/// <summary>Маппинг конвенции ввода `@` ↔ ValueKind для полей Docker-форм (эталон — EmailSendNodeForm).</summary>
public static class DockerNodeFormUtil
{
    public static string KindValue(string kind, string value) => kind switch
    {
        InputValueKind.Expression => "@" + value,
        InputValueKind.Msg => "@msg." + value,
        _ => value,
    };

    public static void SetKinded(string value, Action<string> setKind, Action<string> setValue)
    {
        if (value.StartsWith('@'))
        {
            setKind(InputValueKind.Expression);
            setValue(value[1..].Trim());
        }
        else
        {
            setKind(InputValueKind.Const);
            setValue(value);
        }
    }
}
