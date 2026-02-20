using Codice.Client.Common.WebApi.Requests;
using UnityEngine;

public interface ILLMProvider
{
    public string SendRequest(string userInput);
}
