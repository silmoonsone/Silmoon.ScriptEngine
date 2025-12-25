#pragma f "IStorage.cs"
#pragma f "Storage.cs"

using System.Text;

namespace ScriptEngine.TestingCode;

public class MyStorage : Storage, IMy
{
    public MyStorage()
    {

    }
    public void SetName(string name)
    {
        Set("Name", Encoding.UTF8.GetBytes(name));
    }
    public string GetName()
    {
        return Encoding.UTF8.GetString(Get("Name"));
    }
    ~MyStorage()
    {
        Console.WriteLine("Finalized");
    }
}
public interface IMy
{

}