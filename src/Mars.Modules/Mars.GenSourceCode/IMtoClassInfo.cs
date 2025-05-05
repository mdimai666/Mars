using System.Text;

namespace Mars.GenSourceCode;

public interface IMtoClassInfo
{
    public void WriteTo(StringBuilder sb, bool closeBrackets = true);
    public void WriteSelectExpression(StringBuilder sb);

}
