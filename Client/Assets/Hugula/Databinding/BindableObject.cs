// Copyright (c) 2020 hugula
// direct https://github.com/tenvick/hugula
//
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Hugula.Databinding {

	public abstract class BindableObject : MonoBehaviour, INotifyPropertyChanged {

		public const string EnabledProperty = "enabled";
		public const string TagProperty = "tag";

		public const string ContextProperty = "context";

		#region  重写属性
		public new bool enabled {
			get { return base.enabled; }
			set {
				base.enabled = value;
				OnPropertyChanged ();
			}
		}

		public new string tag {
			get { return base.tag; }
			set {
				base.tag = value;
				OnPropertyChanged ();
			}
		}
		#endregion

		#region  databinding
		[SerializeField]
		private BindableObject m_Parent;
		public BindableObject parent { get { return m_Parent; } }
		protected object m_Context;
		protected object m_InheritedContext;

		public event PropertyChangedEventHandler PropertyChanged;

		internal bool forceContextChanged = false;
		/// <summary>
		/// 绑定上下文
		/// </summary>
		public object context {
			get { return m_InheritedContext?? m_Context; }
			set {
				if (!Object.Equals (m_Context, value) || forceContextChanged) {
					forceContextChanged = false;
					m_InheritedContext = null;
					OnBindingContextChanging ();
					SetProperty<object> (ref m_Context, value);
					OnBindingContextChanged ();
				}
			}
		}

		/// <summary>
		/// 继承的绑定上下文
		/// </summary>
		public object inheritedContext {
			get { return m_InheritedContext; }
			set {
				if (!Object.Equals (m_InheritedContext, value)) {
					SetProperty<object> (ref m_InheritedContext, value);
					OnInheritedContextChanged ();
				}
			}
		}

		[SerializeField]
		UnityEngine.Object m_Target;

		///<summary>
		/// 绑定对象的name
		///</summary>
		public string targetName;

		///<summary>
		/// 对应lua 中Binder的lua class type
		///</summary>
		public string binderType;

		///<summary>
		/// The UnityEngine.Object of the Target
		///</summary>
		public Object target {
			get { return m_Target; } set { m_Target = value; }
		}

		public T GetTarget<T> () where T : UnityEngine.Object {
			return (T) m_Target;
		}

		///<summary>
		/// 绑定表达式
		///<summary>
		[HideInInspector]
		public List<Binding> bindings;

		private bool m_IsbindingsDictionary = false;
		Dictionary<string, Binding> m_BindingsDic = new Dictionary<string, Binding> ();

		public Binding GetBinding (string property) {
			if (!m_IsbindingsDictionary) {
				foreach (var item in bindings)
					m_BindingsDic[item.propertyName] = item;
			}

			Binding binding = null;
			m_BindingsDic.TryGetValue (property, out binding);
			return binding;
		}

		// public void SetBinding (string property, Binding binding) {
		// 	bindings.Add (binding);
		// 	binding.propertyName = property;
		// 	m_BindingsDic[property] = binding;
		// }

		public void SetParent (BindableObject parent) {
			this.m_Parent = parent;
		}

		protected virtual void OnInheritedContextChanged () {

		}

		internal virtual void SetInheritedContext (object value, bool force) {
			if (!Object.Equals (m_InheritedContext, value) || force) {
				m_InheritedContext = value;
				OnBindingContextChanging ();
				var contextBinding = GetBinding (ContextProperty);
				if (contextBinding != null && contextBinding.path != ".") {
					contextBinding.Unapply ();
					contextBinding.target = this;
					contextBinding.context = context;
					contextBinding.Apply (true);
				} else
					OnBindingContextChanged ();
			}
		}
		protected virtual void OnBindingContextChanging () {

		}

		protected virtual void OnBindingContextChanged () {
			//上下文改变执行表达式
			foreach (var binding in bindings) {
				// Debug.LogFormat ("OnBindingContextChanged({0},{1})", binding.propertyName, binding.path);
				if (!ContextProperty.Equals (binding.propertyName)) { //context需要触发自己，由inherited触发
					binding.target = this;
					binding.context = context;
					// Debug.LogFormat ("binding.Apply({0},{1})", binding, context);
					binding.Apply ();
				}
			}
		}

		protected virtual void OnPropertyChanged ([CallerMemberName] string propertyName = null) {
			PropertyChanged?.Invoke (this, propertyName);
		}

		//变化的同时更新目标源
		protected virtual void OnPropertyChangedBindingApply ([CallerMemberName] string propertyName = null) {
			Binding binding = GetBinding (propertyName);
			if (binding != null && binding.mode == "twoway") {
				binding.Apply (false);
			}
			PropertyChanged?.Invoke (this, propertyName);
		}

		protected bool SetProperty<T> (ref T storage, T value, [CallerMemberName] string propertyName = null) {
			if (Object.Equals (storage, value))
				return false;

			storage = value;
			OnPropertyChanged (propertyName);
			return true;
		}

		#endregion

		protected virtual void Start () {

		}

		protected virtual void OnDestroy () {
			foreach (var binding in bindings)
				binding.Dispose ();

			bindings.Clear ();
			m_Target = null;
			m_Context = null;
			m_InheritedContext = null;
			m_Parent = null;
		}

	}
}