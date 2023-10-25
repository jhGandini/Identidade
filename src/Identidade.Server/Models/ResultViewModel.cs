using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Identidade.Server.Models;
public class ResultViewModel : Result
{
    public ResultViewModel(ModelStateDictionary data)
    {
        foreach (var erro in data)
        {
            foreach (var er in erro.Value.Errors)
            {
                AddNotification(erro.Key, er.ErrorMessage);
            }
        }
    }

    public ResultViewModel(IdentityResult data)
    {
        foreach (var erro in data.Errors)
        {
            AddNotification(erro.Code, erro.Description);
        }
    }

    public ResultViewModel(object data, ModelStateDictionary state)
    {
        Data = data;

        foreach (var erro in state)
        {
            foreach (var er in erro.Value.Errors)
            {
                AddNotification(erro.Key, er.ErrorMessage);
            }
        }
    }

    public ResultViewModel(object data, IdentityResult state)
    {
        Data = data;

        foreach (var erro in state.Errors)
        {
            AddNotification(erro.Code, erro.Description);
        }
    }

    public ResultViewModel() { }
    public ResultViewModel(object data) : base(data) { }
    public ResultViewModel(int count) : base(count) { }
    public ResultViewModel(object data, int count) : base(data, count) { }
}
