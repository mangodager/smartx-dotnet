using System;
using System.Collections.Generic;
using System.Threading;

namespace ETModel
{
    public class ThreadLocalID<T>
    {
        Dictionary<int, T> _dic = new Dictionary<int, T>();
        Func<T> _valueFactory;

        public ThreadLocalID(Func<T> valueFactory)
        {
            _valueFactory = valueFactory;
        }

        public T Value
        {
            get
            {
                if (!_dic.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                {
                    lock (_dic)
                    {
                        if (!_dic.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                        {
                            _dic.Add(Thread.CurrentThread.ManagedThreadId, _valueFactory());
                        }
                    }
                }
                return _dic[Thread.CurrentThread.ManagedThreadId];
            }
        }

    }
}
