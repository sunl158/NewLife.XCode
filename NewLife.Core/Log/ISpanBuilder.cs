﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Log
{
    /// <summary>跟踪片段构建器</summary>
    public interface ISpanBuilder
    {
        #region 属性
        /// <summary>跟踪器</summary>
        ITracer Tracer { get; }

        /// <summary>操作名</summary>
        String Name { get; set; }

        /// <summary>开始时间。Unix毫秒</summary>
        Int64 StartTime { get; set; }

        /// <summary>结束时间。Unix毫秒</summary>
        Int64 EndTime { get; set; }

        /// <summary>采样总数</summary>
        Int32 Total { get; }

        /// <summary>错误次数</summary>
        Int32 Errors { get; }

        /// <summary>总耗时。所有请求耗时累加，单位ms</summary>
        Int64 Cost { get; }

        /// <summary>最大耗时。单位ms</summary>
        Int32 MaxCost { get; }

        /// <summary>最小耗时。单位ms</summary>
        Int32 MinCost { get; }

        /// <summary>正常采样</summary>
        IList<ISpan> Samples { get; }

        /// <summary>异常采样</summary>
        IList<ISpan> ErrorSamples { get; }
        #endregion

        #region 方法
        /// <summary>开始一个Span</summary>
        /// <returns></returns>
        ISpan Start();

        /// <summary>完成Span</summary>
        /// <param name="span"></param>
        void Finish(ISpan span);
        #endregion
    }

    /// <summary>跟踪片段构建器</summary>
    public class DefaultSpanBuilder : ISpanBuilder
    {
        #region 属性
        /// <summary>跟踪器</summary>
        [XmlIgnore, ScriptIgnore, IgnoreDataMember]
        public ITracer Tracer { get; }

        /// <summary>操作名</summary>
        public String Name { get; set; }

        /// <summary>开始时间。Unix毫秒</summary>
        public Int64 StartTime { get; set; }

        /// <summary>结束时间。Unix毫秒</summary>
        public Int64 EndTime { get; set; }

        private Int32 _Total;
        /// <summary>采样总数</summary>
        public Int32 Total { get => _Total; private set => _Total = value; }

        private Int32 _Errors;
        /// <summary>错误次数</summary>
        public Int32 Errors { get => _Errors; private set => _Errors = value; }

        private Int64 _Cost;
        /// <summary>总耗时。所有请求耗时累加，单位ms</summary>
        public Int64 Cost { get => _Cost; private set => _Cost = value; }

        /// <summary>最大耗时。单位ms</summary>
        public Int32 MaxCost { get; private set; }

        /// <summary>最小耗时。单位ms</summary>
        public Int32 MinCost { get; private set; }

        /// <summary>正常采样</summary>
        public IList<ISpan> Samples { get; private set; }

        /// <summary>异常采样</summary>
        public IList<ISpan> ErrorSamples { get; private set; }
        #endregion

        #region 
        /// <summary>实例化</summary>
        public DefaultSpanBuilder() { }

        /// <summary>实例化</summary>
        /// <param name="tracer"></param>
        /// <param name="name"></param>
        public DefaultSpanBuilder(ITracer tracer, String name)
        {
            Tracer = tracer;
            Name = name;
            MinCost = -1;

            StartTime = DateTime.UtcNow.ToLong();
        }
        #endregion

        #region 方法
        /// <summary>开始一个Span</summary>
        /// <returns></returns>
        public virtual ISpan Start()
        {
            var span = new DefaultSpan(this);
            span.Start();

            return span;
        }

        /// <summary>完成Span</summary>
        /// <param name="span"></param>
        public virtual void Finish(ISpan span)
        {
            // 总次数
            var total = Interlocked.Increment(ref _Total);

            // 累计耗时
            var cost = (Int32)(span.EndTime - span.StartTime);
            Interlocked.Add(ref _Cost, cost);

            // 最大最小耗时
            if (MaxCost < cost) MaxCost = cost;
            if (MinCost > cost || MinCost < 0) MinCost = cost;

            // 处理采样
            if (span.Error != null)
            {
                if (Interlocked.Increment(ref _Errors) <= Tracer.MaxErrors)
                {
                    var ss = ErrorSamples ??= new List<ISpan>();
                    lock (ss)
                    {
                        ss.Add(span);
                    }
                }
            }
            else
            {
                if (total <= Tracer.MaxSamples)
                {
                    var ss = Samples ??= new List<ISpan>();
                    lock (ss)
                    {
                        ss.Add(span);
                    }
                }
            }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => Name;
        #endregion
    }
}