using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Timers;

namespace ETModel
{
public class TimePass
{
    public TimePass(float start,float end)
    {
        timeStart = start;
        timeEnd   = end;
        time      = TimeHelper.time;
        isPass    = false;
        isStart   = false;
    }
    public TimePass(float end)
    {
        timeStart = 0;
        timeEnd   = end;
        time      = TimeHelper.time;
        isPass    = false;
        isStart   = false;
    }

    public void Do(float start,float end)
    {
        timeStart = start;
        timeEnd   = end;
        time      = TimeHelper.time;
        isPass    = false;
        isStart   = false;
    }

    public bool IsPassSet()
    {
        bool b = (TimeHelper.time - time-timeStart) > timeEnd;
        if(b)
        {
            time = TimeHelper.time;
        }
        return b;
    }
    public bool IsPass()
    {
        return (TimeHelper.time - time-timeStart) > timeEnd;
    }
    public bool IsBefore()
    {
        return TimeHelper.time - time < timeStart;
    }
    public void Reset()
    {
        time     = TimeHelper.time;
        isPass   = false;
        isStart  = false;
    }
    public bool IsPassOnce()
    {
        if(isPass)
            return false;
        isPass = (TimeHelper.time - time-timeStart) > timeEnd;
        return isPass;
    }

    public bool IsStartOnce()
    {
        if(isStart)
            return false;
        isStart = TimeHelper.time - time >= timeStart;
        return isStart;
    }
    public bool IsStart()
    {
        return TimeHelper.time - time >= timeStart;
    }

    public float percent()
    {
        float per = 0;
        if( (TimeHelper.time - time-timeStart) > timeEnd)
        {
            per = 1;
        }
        if( (TimeHelper.time - time-timeStart) > 0 )
        {
            per = (TimeHelper.time - time-timeStart) / timeEnd;
        }
        return per;
    }

    public float timeStart;
    public float timeEnd;
    public float time;
    public bool  isPass;
    public bool  isStart;

}



}