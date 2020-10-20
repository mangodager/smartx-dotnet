using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETModel
{
    /// <summary>
    /// 等待接口
    /// </summary>
    public interface IWait
    {
        /// <summary>
        /// 每帧检测是否等待结束
        /// </summary>
        /// <returns></returns>
        bool Tick();
    }

    /// <summary>
    /// 按秒等待
    /// </summary>
    public class WaitForSeconds : IWait
    {
        // 每帧的时间
        public static float deltaTime { get { return (float) (20f / 1000f); } }

        float _seconds = 0f;

        public WaitForSeconds(float seconds)
        {
            _seconds = seconds;
        }

        public bool Tick()
        {
            _seconds -= TimeHelper.deltaTime;
            return _seconds <= 0;
        }
    }

    /// <summary>
    /// 按帧等待
    /// </summary>
    public class WaitForFrames : IWait
    {
        private int _frames = 0;
        public WaitForFrames(int frames)
        {
            _frames = frames;
        }

        public bool Tick()
        {
            _frames -= 1;
            return _frames <= 0;
        }
    }

    public class CoroutineMgr
    {
        private static CoroutineMgr _instance = null;
        private static CoroutineMgr Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CoroutineMgr();
                }
                return _instance;
            }
        }

        private LinkedList<IEnumerator> coroutineList = new LinkedList<IEnumerator>();

        static public void StartCoroutine(IEnumerator routine)
        {
            Instance.coroutineList.AddLast(routine);
        }
        
        static public void StopCoroutine(IEnumerator routine)
        {
            try
            {
                Instance.coroutineList.Remove(routine);
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
        }

        static public void UpdateCoroutine()
        {
            var node = Instance.coroutineList.First;
            while (node != null)
            {
                IEnumerator ie = node.Value;
                bool ret = true;
                if (ie.Current is IWait)
                {
                    IWait wait = (IWait)ie.Current;
                    //检测等待条件，条件满足，跳到迭代器的下一元素 （IEnumerator方法里的下一个yield）
                    if (wait.Tick())
                    {
                        ret = ie.MoveNext();
                    }
                }
                else
                {
                    ret = ie.MoveNext();
                }
                //迭代器没有下一个元素了，删除迭代器（IEnumerator方法执行结束）
                if (!ret)
                {
                    Instance.coroutineList.Remove(node);
                }
                //下一个迭代器
                node = node.Next;
            }
        }
    }



}