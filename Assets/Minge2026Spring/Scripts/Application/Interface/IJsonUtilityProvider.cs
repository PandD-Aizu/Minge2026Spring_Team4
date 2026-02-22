namespace Minge2026Spring.Scripts.Application.Interface
{
    public interface IJsonUtilityProvider
    {
        public T ConvertJsonToAnyObject<T>(string addressableJsonKey);
        public string ConvertStringToJson<T>(T data, bool prettyPrint = false);
    }
}