using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aardvark.Runtime;
using Aardvark.SceneGraph;

namespace SimpleViewer
{
    public static partial class MySg
    {
        public class EnvSelector : Instance
        {
            public const string Identifier = "EnvSelector";

            public static class Property
            {
                public const string EnvName = "EnvName";
                public const string InitialChoice = "InitialChoice";
                public const string SceneGraphArray = "SceneGraphArray";
            }

            public EnvSelector()
                : base(EnvSelector.Identifier)
            {
            }

            public EnvSelector(string typeName)
                : base(typeName)
            {
            }

            public string EnvName
            {
                get { return Get<string>(Property.EnvName); }
                set { this[Property.EnvName] = value; }
            }

            public int InitialChoice
            {
                get { return Get<int>(Property.InitialChoice); }
                set { this[Property.InitialChoice] = value; }
            }

            public ISg[] SceneGraphArray
            {
                get { return Get<ISg[]>(Property.SceneGraphArray); }
                set { this[Property.SceneGraphArray] = value; }
            }
        }
    }

    [Rule(typeof(MySg.EnvSelector))]
    public class EnvSelectorRule : IRule
    {
        protected MySg.EnvSelector m_instance;
        protected string m_envName;
        protected int m_choice;
        protected HalfLeaf m_rsg;
        protected ISg m_allChildren;

        public EnvSelectorRule(MySg.EnvSelector instance, AbstractTraversal traversal)
        {
            m_instance = instance;
            m_choice = instance.InitialChoice;
            m_rsg = new HalfLeaf(instance.SceneGraphArray[m_choice]);
            m_envName = instance.EnvName;
            m_allChildren = Sg.Group(m_instance.SceneGraphArray);
        }

        #region IRule Members

        public virtual void InitForPath(AbstractTraversal traversal)
        {
            //nop
        }

        virtual public ISg SetParameters(AbstractTraversal traversal)
        {
            if ((traversal is Aardvark.SceneGraph.BuildCachesTraversal)
                || (traversal is Aardvark.SceneGraph.WaitStreamingTraversal))
                return m_allChildren;

            m_choice = traversal.EnvironmentMap.Get<int>(m_envName, m_instance.InitialChoice);
            m_rsg.Child = m_instance.SceneGraphArray[m_choice];
            return m_rsg;
        }

        #endregion

        #region IDisposeAndRemove

        public bool DisposeAndRemove(DisposeAndRemoveTraversal t)
        {
            t.TryDisposeAndRemoveRule(m_instance, t, m_rsg);
            return true;
        }

        #endregion
    }
}
