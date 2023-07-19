using Flunt.Notifications;

namespace Serede.Identidade.Models;

public class Result : Notifiable<Notification>
{
    public Object Data { get; set; }
    public int Count { get; set; }

    public Result(object data)
    {
        Data = data;
    }

    public Result(int count)
    {
        Count = count;
    }

    public Result(object data, int count)
    {
        Data = data;
        Count = count;
    }

    public Result() { }
}
