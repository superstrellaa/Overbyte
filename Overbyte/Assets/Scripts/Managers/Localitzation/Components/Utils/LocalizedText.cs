[System.Serializable]
public class LocalizedText
{
    private string key;

    public LocalizedText(string key)
    {
        this.key = key;
    }

    public string Value => LocalitzationManager.Instance.GetKey(key);

    public void SetKey(string newKey) => key = newKey;
}
