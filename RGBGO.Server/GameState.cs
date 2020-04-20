using MSIGS.Server;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSIGS.Server
{
    class GameState: IGameState
    {

        public GameState()
        {
            this.States = new GameStateDictionary();
        }

        public GameState(string authKey): this()
        {
            this.AuthKey = authKey;
        }

        public string AuthKey { get; set; }
        public GameStateDictionary States { get; private set; }

        public void Read(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException();

            if (!string.IsNullOrEmpty(AuthKey) && !json.Contains(AuthKey))
                throw new UnauthorizedAccessException();

            DeserializeAndFlatten(json);

            States.PurgeChanges();

            OnStateChanged(this, EventArgs.Empty);
        }

        private void DeserializeAndFlatten(string json)
        {
            JToken token = JToken.Parse(json);
            FillDictionaryFromJToken(token, "");
        }

        // Thanks Brian Rogers for sharing this on Stackoverflow!
        private void FillDictionaryFromJToken(JToken token, string prefix)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty prop in token.Children<JProperty>())
                    {
                        FillDictionaryFromJToken(prop.Value, Join(prefix, prop.Name));
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (JToken value in token.Children())
                    {
                        FillDictionaryFromJToken(value, Join(prefix, index.ToString()));
                        index++;
                    }
                    break;

                default:
                    this.States[prefix] =  ((JValue)token).Value.ToString();
                    break;
            }
        }

        private string Join(string prefix, string name)
        {
            return (string.IsNullOrEmpty(prefix) ? name : prefix + "." + name);
        }

        public event EventHandler OnStateChanged;

        protected virtual void OnStateChangedEvent(EventArgs e)
        {
            OnStateChanged?.Invoke(this, e);
        }
    }

    public interface IGameState
    {
        event EventHandler OnStateChanged;
        void Read(string data);
        string AuthKey { get; set; }
        GameStateDictionary States { get; }
    }

    public class GameStateDictionary : IDictionary<string, string>
    {

        public GameStateDictionary()
        {
            this.Changed = new List<string>();
        }

        public List<string> Changed { get; private set; }

        private Dictionary<string, string> dictionary = new Dictionary<string, string>();

        public string this[string key] { 
            get { return dictionary.ContainsKey(key) ? dictionary[key] : null; } 
            set { TrackChange(key, value); dictionary[key] = value; } 
        }

        public ICollection<string> Keys => dictionary.Keys;

        public ICollection<string> Values => dictionary.Values;

        public int Count => dictionary.Count;

        public bool IsReadOnly => false;

        public void Add(string key, string value)
        {
            TrackChange(key, value);

            dictionary.Add(key, value);
        }

        private void TrackChange(string key, string value)
        {
            if (!dictionary.ContainsKey(key) || dictionary[key] != value)
            {
                if (!Changed.Contains(key))
                    Changed.Add(key);
            } else
            {
                Changed.Remove(key);
            }    
        }

        public void PurgeChanges()
        {
            for (int i = Changed.Count - 1; i >= 0; i--)
            {
                if (!ContainsKey(Changed[i]))
                    Changed.RemoveAt(i);
            }
        }

        public void Add(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(string key)
        {
            return dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(string key)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out string value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public bool HasChanged(string key)
        {
            return Changed.Contains(key);
        }
    }

}
