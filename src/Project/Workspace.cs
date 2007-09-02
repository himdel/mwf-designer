//
// Authors:	 
//	  Ivan N. Zlatev (contact i-nZ.net)
//
// (C) 2007 Ivan N. Zlatev

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Reflection;
using System.Drawing.Design;

#if WITH_MONO_DESIGN
using Mono.Design;
#endif

namespace mwf_designer
{
	internal class Workspace
	{

		private List<Document> _documents;
		private Document _activeDocument;
		private References _references;
		private ServiceContainer _serviceContainer;

		public Workspace ()
		{
			_documents = new List<Document>();
			_activeDocument = null;
			_references = new References ();
			_serviceContainer = new ServiceContainer ();
		}

		public void Load ()
		{
			LoadDefaultReferences (_references);
			LoadDefaultServices (_serviceContainer);
		}

		private void LoadDefaultServices (IServiceContainer container)
		{
			container.AddService (typeof (ITypeResolutionService), new TypeResolutionService (this.References));
		}

		private void LoadDefaultReferences (References references)
		{
			references.AddReference (Assembly.Load ("System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
			references.AddReference (Assembly.Load ("System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
			references.AddReference (Assembly.Load ("System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
		}

		public References References {
			get { return _references; }
		}

		public void AddDocument (Document doc)
		{
			_documents.Add (doc);
		}

		public void RemoveDocument (Document doc)
		{
			_documents.Remove (doc);
		}

		public ServiceContainer Services {
			get { return _serviceContainer; }
		}

		public Document ActiveDocument {
			get { return _activeDocument; }
			set { 
				Document old = _activeDocument;
				_activeDocument = value;
				OnActiveDocumentChanged (value, old);
			}
		}


		private static readonly Type[] containers = new Type[] {
			typeof (System.Windows.Forms.FlowLayoutPanel),
			typeof (System.Windows.Forms.GroupBox),
			typeof (System.Windows.Forms.Panel),
			typeof (System.Windows.Forms.SplitContainer),
			typeof (System.Windows.Forms.TabControl),
			typeof (System.Windows.Forms.TableLayoutPanel),
		};

		private static readonly Type[] menusToolbars = new Type[] {
			typeof (System.Windows.Forms.ContextMenuStrip),
			typeof (System.Windows.Forms.MenuStrip),
			typeof (System.Windows.Forms.StatusStrip),
			typeof (System.Windows.Forms.ToolStrip),
			typeof (System.Windows.Forms.ToolStripContainer),
		};

		private static readonly Type[] commonControls = new Type[] {
			typeof (System.Windows.Forms.Button),
			typeof (System.Windows.Forms.CheckBox),
			typeof (System.Windows.Forms.CheckedListBox),
			typeof (System.Windows.Forms.ComboBox),
			typeof (System.Windows.Forms.DateTimePicker),
			typeof (System.Windows.Forms.Label),
			typeof (System.Windows.Forms.LinkLabel),
			typeof (System.Windows.Forms.ListBox),
			typeof (System.Windows.Forms.ListView),
			typeof (System.Windows.Forms.MaskedTextBox),
			typeof (System.Windows.Forms.MonthCalendar),
			typeof (System.Windows.Forms.NotifyIcon),
			typeof (System.Windows.Forms.NumericUpDown),
			typeof (System.Windows.Forms.PictureBox),
			typeof (System.Windows.Forms.ProgressBar),
			typeof (System.Windows.Forms.RadioButton),
			typeof (System.Windows.Forms.RichTextBox),
			typeof (System.Windows.Forms.TextBox),
			typeof (System.Windows.Forms.ToolTip),
			typeof (System.Windows.Forms.TreeView),
			typeof (System.Windows.Forms.WebBrowser),
		};

		private static readonly Type[] commonComponents = new Type[] {
			typeof (System.ComponentModel.BackgroundWorker),
			typeof (System.IO.FileSystemWatcher),
			typeof (System.Diagnostics.Process),
			typeof (System.Windows.Forms.ImageList),
			typeof (System.Windows.Forms.HelpProvider),
			typeof (System.IO.Ports.SerialPort),
			typeof (System.Windows.Forms.Timer),
		};


		public List<ToolboxItem> GetToolboxItems ()
		{
			List<ToolboxItem> list = new List<ToolboxItem>();

			foreach (Assembly assembly in _references.Assemblies) {
				foreach (Type type in assembly.GetTypes()) {
					if (IsValidToolType (type) && HasPublicEmptyCtor (type)) {
						ToolboxItem tool = new ToolboxItem (type);
						tool.Properties["Category"] = "All";
						list.Add (tool);
					}
				}
			}

			foreach (Type type in commonComponents) {
				ToolboxItem tool = new ToolboxItem (type);
				tool.Properties["Category"] = "Common Components";
				list.Add (tool);
			}

			foreach (Type type in menusToolbars) {
				ToolboxItem tool = new ToolboxItem (type);
				tool.Properties["Category"] = "Menus and Toolbars";
				list.Add (tool);
			}

			foreach (Type type in commonControls) {
				ToolboxItem tool = new ToolboxItem (type);
				tool.Properties["Category"] = "Common Controls";
				list.Add (tool);
			}

			foreach (Type type in containers) {
				ToolboxItem tool = new ToolboxItem (type);
				tool.Properties["Category"] = "Containers";
				list.Add (tool);
			}

			return list;
		}

		private bool HasPublicEmptyCtor (Type type)
		{
			bool result = false;
			ConstructorInfo[] ctors = type.GetConstructors ();
			if (ctors.Length > 0) {
				foreach (ConstructorInfo ctor in ctors)
					if (ctor.GetParameters().Length == 0)
						result = true;
			}
			return result;
		}

		private bool IsValidToolType (Type type)
		{
			ToolboxItemAttribute toolboxAttribute = TypeDescriptor.GetAttributes (type)[typeof (ToolboxItemAttribute)] as ToolboxItemAttribute;
			if (toolboxAttribute.ToolboxItemTypeName != ToolboxItemAttribute.None.ToolboxItemTypeName &&
				!type.IsAbstract && !type.IsInterface &&
				((type.Attributes & TypeAttributes.Public) == TypeAttributes.Public) && 
				((type.Attributes & TypeAttributes.NestedFamily) != TypeAttributes.NestedFamily) && 
				((type.Attributes & TypeAttributes.NestedFamORAssem) != TypeAttributes.NestedFamORAssem) && 
				typeof (System.Windows.Forms.Control).IsAssignableFrom (type))
				return true;
			return false;
		}

		protected virtual void OnActiveDocumentChanged (Document newDocument, Document oldDocument)
		{
			if (ActiveDocumentChanged != null)
				ActiveDocumentChanged (this, new ActiveDocumentChangedEventArgs (newDocument, oldDocument));
		}

		public event ActiveDocumentChangedEventHandler ActiveDocumentChanged;
	}


	internal class ActiveDocumentChangedEventArgs : EventArgs
	{
		private Document _oldDocument;
		private Document _newDocument;

		public ActiveDocumentChangedEventArgs (Document newDocument, Document oldDocument)
		{
			if (newDocument == null)
				throw new ArgumentNullException ("newDocument");

			_newDocument = newDocument;
			_oldDocument = oldDocument;
		}

		public Document OldDocument {
			get { return _oldDocument; }
			set { _oldDocument = value; }
		}

		public Document NewDocument {
			get { return _newDocument; }
			set { _newDocument = value; }
		}
	}

	internal delegate void ActiveDocumentChangedEventHandler (object sender, ActiveDocumentChangedEventArgs args);

}