﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Reflex
{
    public class TestClass<T>
    {
        public T prop { get; set; }

        //public static TestClass operator +(TestClass a, TestClass b)
        //{
        //    return a;
        //}
        //public static TestClass operator -(TestClass a, TestClass b)
        //{
        //    return a;
        //}
        //public static TestClass operator *(TestClass a, TestClass b)
        //{
        //    return a;
        //}
        //public static TestClass operator /(TestClass a, TestClass b)
        //{
        //    return a;
        //}

        //public static implicit operator string(TestClass tc)
        //{
        //    return "";
        //}

        //public static implicit operator TestClass(string s)
        //{
        //    return new TestClass();
        //}
        
        //public async void PubAsyncVoid()
        //{
        //    await Task.Run((() => PrivProp++));
        //}
    }
}
