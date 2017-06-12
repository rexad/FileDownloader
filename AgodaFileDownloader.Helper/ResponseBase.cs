using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AgodaFileDownloader.Helper
{
    public class ResponseBase
    {

        public ResponseBase()
        {
            Messages = new Collection<string>();
        }

        public ICollection<string> Messages { get; private set; }
        public bool Denied { get; set; }

        public void AddMessage(string message)
        {
            Messages.Add(message);
        }

        public void AddListMessage(IEnumerable<string> messages)
        {
            foreach (var message in messages)
            {
                Messages.Add(message);
            }
        }
    }
    public class ResponseBase<T> : ResponseBase
    {
        public T ReturnedValue;
    }
}
