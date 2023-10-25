using Flunt.Notifications;

namespace Identidade.Server.Models;

public abstract class Result : Notifiable<Notification>
{
    public object Data { get; set; }
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
