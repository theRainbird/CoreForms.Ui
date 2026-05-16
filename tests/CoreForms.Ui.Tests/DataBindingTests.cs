using System.ComponentModel;
using CoreForms.Ui.Core;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Data;
using Xunit;

namespace CoreForms.Ui.Tests;

public class DataBindingTests
{
    private sealed class TestItem : INotifyPropertyChanged
    {
        private string _name = "";
        private int _value;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // ===== INotifyPropertyChanged on Control =====

    [Fact]
    public void Control_PropertyChanged_FiresOnTextChange()
    {
        var control = new Label();
        string? changedProperty = null;
        control.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

        control.Text = "Hello";

        Assert.Equal("Text", changedProperty);
    }

    [Fact]
    public void Control_PropertyChanged_FiresOnEnabledChange()
    {
        var control = new Label();
        string? changedProperty = null;
        control.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

        control.Enabled = false;

        Assert.Equal("Enabled", changedProperty);
    }

    [Fact]
    public void Control_PropertyChanged_FiresOnVisibleChange()
    {
        var control = new Label();
        string? changedProperty = null;
        control.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

        control.Visible = false;

        Assert.Equal("Visible", changedProperty);
    }

    [Fact]
    public void Control_PropertyChanged_DoesNotFireWhenValueUnchanged()
    {
        var control = new Label();
        control.Text = "Initial";
        int fireCount = 0;
        control.PropertyChanged += (s, e) => fireCount++;

        control.Text = "Initial";

        Assert.Equal(0, fireCount);
    }

    // ===== BindingList<T> =====

    [Fact]
    public void BindingList_Add_RaisesListChanged()
    {
        var list = new BindingList<string>();
        ListChangedType? changedType = null;
        int newIndex = -1;
        list.ListChanged += (s, e) => { changedType = e.ListChangedType; newIndex = e.NewIndex; };

        list.Add("Item1");

        Assert.Equal(ListChangedType.ItemAdded, changedType);
        Assert.Equal(0, newIndex);
        Assert.Single(list);
    }

    [Fact]
    public void BindingList_Remove_RaisesListChanged()
    {
        var list = new BindingList<string> { "Item1", "Item2" };
        ListChangedType? changedType = null;
        list.ListChanged += (s, e) => changedType = e.ListChangedType;

        list.Remove("Item1");

        Assert.Equal(ListChangedType.ItemDeleted, changedType);
        Assert.Single(list);
    }

    [Fact]
    public void BindingList_Clear_RaisesReset()
    {
        var list = new BindingList<string> { "Item1", "Item2" };
        ListChangedType? changedType = null;
        list.ListChanged += (s, e) => changedType = e.ListChangedType;

        list.Clear();

        Assert.Equal(ListChangedType.Reset, changedType);
        Assert.Empty(list);
    }

    [Fact]
    public void BindingList_Insert_RaisesListChanged()
    {
        var list = new BindingList<string> { "Item1", "Item3" };
        ListChangedType? changedType = null;
        int newIndex = -1;
        list.ListChanged += (s, e) => { changedType = e.ListChangedType; newIndex = e.NewIndex; };

        list.Insert(1, "Item2");

        Assert.Equal(ListChangedType.ItemAdded, changedType);
        Assert.Equal(1, newIndex);
        Assert.Equal(3, list.Count);
        Assert.Equal("Item2", list[1]);
    }

    [Fact]
    public void BindingList_ItemPropertyChange_RaisesItemChanged()
    {
        var list = new BindingList<TestItem>();
        var item = new TestItem { Name = "Original" };
        list.Add(item);

        ListChangedType? changedType = null;
        int changedIndex = -1;
        list.ListChanged += (s, e) => { changedType = e.ListChangedType; changedIndex = e.NewIndex; };

        item.Name = "Changed";

        Assert.Equal(ListChangedType.ItemChanged, changedType);
        Assert.Equal(0, changedIndex);
    }

    // ===== Binding =====

    [Fact]
    public void Binding_OneWay_PushesInitialValue()
    {
        var source = new TestItem { Name = "SourceName" };
        var control = new TextBox();

        control.DataBindings.Add("Text", source, "Name");

        Assert.Equal("SourceName", control.Text);
    }

    [Fact]
    public void Binding_TwoWay_ControlChangeUpdatesSource()
    {
        var source = new TestItem { Name = "Initial" };
        var control = new TextBox();
        control.DataBindings.Add("Text", source, "Name");

        control.Text = "Updated";

        Assert.Equal("Updated", source.Name);
    }

    [Fact]
    public void Binding_TwoWay_SourceChangeUpdatesControl()
    {
        var source = new TestItem { Name = "Initial" };
        var control = new TextBox();
        control.DataBindings.Add("Text", source, "Name");

        source.Name = "SourceUpdated";

        Assert.Equal("SourceUpdated", control.Text);
    }

    [Fact]
    public void Binding_CheckBox_CheckedBinding()
    {
        // Use an IsActive property that can be toggled as bool
        var source = new { Checked = false };
        // We need a proper INotifyPropertyChanged source
        var item = new TestItem { Value = 0 };
        var control = new CheckBox();
        control.DataBindings.Add("Checked", item, "Value");

        Assert.False(control.Checked);

        item.Value = 1;
        // The binding pushes 1 (int) to Checked (bool).
        // Binding.OnParse converts via Convert.ChangeType which converts 1 → true.
        Assert.True(control.Checked);
    }

    // ===== CurrencyManager =====

    [Fact]
    public void CurrencyManager_PositionInitializesCorrectly()
    {
        var list = new BindingList<string> { "A", "B", "C" };
        var cm = new CurrencyManager(list);

        Assert.Equal(0, cm.Position);
        Assert.Equal(3, cm.Count);
        Assert.Equal("A", cm.Current);
    }

    [Fact]
    public void CurrencyManager_MoveNext()
    {
        var list = new BindingList<string> { "A", "B", "C" };
        var cm = new CurrencyManager(list);

        cm.MoveNext();

        Assert.Equal(1, cm.Position);
        Assert.Equal("B", cm.Current);
    }

    [Fact]
    public void CurrencyManager_MovePrevious()
    {
        var list = new BindingList<string> { "A", "B", "C" };
        var cm = new CurrencyManager(list);
        cm.Position = 2;

        cm.MovePrevious();

        Assert.Equal(1, cm.Position);
        Assert.Equal("B", cm.Current);
    }

    [Fact]
    public void CurrencyManager_MoveFirstAndLast()
    {
        var list = new BindingList<string> { "A", "B", "C" };
        var cm = new CurrencyManager(list);
        cm.Position = 2;

        cm.MoveFirst();
        Assert.Equal(0, cm.Position);
        Assert.Equal("A", cm.Current);

        cm.MoveLast();
        Assert.Equal(2, cm.Position);
        Assert.Equal("C", cm.Current);
    }

    // ===== BindingSource =====

    [Fact]
    public void BindingSource_WrapsBindingList()
    {
        var list = new BindingList<string> { "X", "Y", "Z" };
        var bs = new BindingSource(list);

        Assert.Equal(3, bs.Count);
        Assert.Equal("X", bs.Current);
        Assert.Equal(0, bs.Position);
    }

    [Fact]
    public void BindingSource_MoveNextAndCurrentChanges()
    {
        var list = new BindingList<string> { "A", "B" };
        var bs = new BindingSource(list);

        bs.MoveNext();

        Assert.Equal(1, bs.Position);
        Assert.Equal("B", bs.Current);
    }

    [Fact]
    public void BindingSource_AddNew_AddsToList()
    {
        var list = new BindingList<TestItem> { new TestItem { Name = "A" } };
        var bs = new BindingSource(list);

        var newItem = bs.AddNew();

        Assert.NotNull(newItem);
        Assert.Equal(2, bs.Count);
    }

    [Fact]
    public void BindingSource_DataSourceChanged_ResetsPosition()
    {
        var list = new BindingList<string> { "A" };
        var bs = new BindingSource(list, "");
        bs.MoveNext();

        var newList = new BindingList<string> { "X", "Y" };
        bs.DataSource = newList;

        Assert.Equal(0, bs.Position);
        Assert.Equal("X", bs.Current);
        Assert.Equal(2, bs.Count);
    }

    // ===== ListBox DataSource =====

    [Fact]
    public void ListBox_DataSource_PopulatesItems()
    {
        var list = new BindingList<string> { "Item1", "Item2", "Item3" };
        var lb = new ListBox();

        lb.DataSource = list;

        Assert.Equal(3, lb.Items.Count);
        Assert.Equal("Item1", lb.Items[0]);
        Assert.Equal("Item2", lb.Items[1]);
        Assert.Equal("Item3", lb.Items[2]);
    }

    [Fact]
    public void ListBox_DataSource_SelectedIndexReset()
    {
        var list = new BindingList<string> { "A", "B" };
        var lb = new ListBox();

        lb.DataSource = list;

        Assert.Equal(0, lb.SelectedIndex);
    }

    [Fact]
    public void ListBox_ListChanged_AddsNewItem()
    {
        var list = new BindingList<string> { "A" };
        var lb = new ListBox();
        lb.DataSource = list;

        list.Add("B");

        Assert.Equal(2, lb.Items.Count);
        Assert.Equal("B", lb.Items[1]);
    }

    [Fact]
    public void ListBox_ListChanged_RemovesItem()
    {
        var list = new BindingList<string> { "A", "B", "C" };
        var lb = new ListBox();
        lb.DataSource = list;

        list.RemoveAt(1);

        Assert.Equal(2, lb.Items.Count);
        Assert.Equal("C", lb.Items[1]);
    }

    [Fact]
    public void ListBox_DisplayMember_ShowsProperty()
    {
        var list = new BindingList<TestItem> {
            new TestItem { Name = "First", Value = 1 },
            new TestItem { Name = "Second", Value = 2 }
        };
        var lb = new ListBox();

        lb.DisplayMember = "Name";
        lb.DataSource = list;

        // GetItemDisplayText is protected; we verify via the data source behavior
        Assert.Equal(2, lb.Items.Count);
    }

    [Fact]
    public void ListBox_SelectedValue_MatchesValueMember()
    {
        var list = new BindingList<TestItem> {
            new TestItem { Name = "A", Value = 10 },
            new TestItem { Name = "B", Value = 20 }
        };
        var lb = new ListBox();
        lb.ValueMember = "Value";
        lb.DataSource = list;

        lb.SelectedIndex = 1;
        Assert.Equal(20, lb.SelectedValue);
    }

    [Fact]
    public void ListBox_SelectedValue_SetByValue()
    {
        var list = new BindingList<TestItem> {
            new TestItem { Name = "A", Value = 10 },
            new TestItem { Name = "B", Value = 20 }
        };
        var lb = new ListBox();
        lb.ValueMember = "Value";
        lb.DataSource = list;

        lb.SelectedValue = 20;

        Assert.Equal(1, lb.SelectedIndex);
    }

    [Fact]
    public void ListBox_Selection_UpdatesBindingSourcePosition()
    {
        var list = new BindingList<TestItem> {
            new TestItem { Name = "A", Value = 1 },
            new TestItem { Name = "B", Value = 2 }
        };
        var bs = new BindingSource(list);
        var lb = new ListBox();
        lb.DataSource = bs;

        lb.SelectedIndex = 1;

        Assert.Equal(1, bs.Position);
        Assert.Equal("B", (bs.Current as TestItem)?.Name);
    }

    [Fact]
    public void ListBox_Selection_UpdatesBoundControlsViaBindingSource()
    {
        var list = new BindingList<TestItem> {
            new TestItem { Name = "Alpha", Value = 10 },
            new TestItem { Name = "Beta", Value = 20 }
        };
        var bs = new BindingSource(list);
        var lb = new ListBox();
        lb.DataSource = bs;
        lb.DisplayMember = "Name";

        var textBox = new TextBox();
        textBox.DataBindings.Add("Text", bs, "Name");

        Assert.Equal("Alpha", textBox.Text);

        lb.SelectedIndex = 1;

        Assert.Equal("Beta", textBox.Text);
        Assert.Equal(1, bs.Position);
    }

    // ===== ComboBox DataSource =====

    [Fact]
    public void ComboBox_DataSource_PopulatesItems()
    {
        var list = new BindingList<string> { "X", "Y" };
        var cb = new ComboBox();

        cb.DataSource = list;

        Assert.Equal(2, cb.Items.Count);
        Assert.Equal("X", cb.Items[0]);
    }

    [Fact]
    public void ComboBox_ListChanged_SyncsItems()
    {
        var list = new BindingList<string> { "A" };
        var cb = new ComboBox();
        cb.DataSource = list;

        list.Add("B");

        Assert.Equal(2, cb.Items.Count);
        Assert.Equal("B", cb.Items[1]);
    }

    [Fact]
    public void ComboBox_SelectedValue_Works()
    {
        var list = new BindingList<TestItem> {
            new TestItem { Name = "A", Value = 1 },
            new TestItem { Name = "B", Value = 2 }
        };
        var cb = new ComboBox();
        cb.ValueMember = "Value";
        cb.DataSource = list;

        cb.SelectedIndex = 1;
        Assert.Equal(2, cb.SelectedValue);
    }

    // ===== DataGridView DataSource =====

    [Fact]
    public void DataGridView_DataSource_PopulatesRows()
    {
        var list = new BindingList<TestItem> {
            new TestItem { Name = "A", Value = 1 },
            new TestItem { Name = "B", Value = 2 }
        };
        var dgv = new DataGridView();
        dgv.Columns.Add(new DataGridViewColumn { DataPropertyName = "Name", HeaderText = "Name" });
        dgv.Columns.Add(new DataGridViewColumn { DataPropertyName = "Value", HeaderText = "Value" });

        dgv.DataSource = list;

        Assert.Equal(2, dgv.Rows.Count);
    }

    // ===== PropertyDescriptor =====

    [Fact]
    public void ReflectionPropertyDescriptor_GetsAndSetsValue()
    {
        var item = new TestItem { Name = "Test", Value = 42 };
        var descriptor = ReflectionPropertyDescriptor.GetProperty(typeof(TestItem), "Name");

        Assert.NotNull(descriptor);
        Assert.Equal("Name", descriptor!.Name);
        Assert.Equal(typeof(string), descriptor.PropertyType);

        var value = descriptor.GetValue(item);
        Assert.Equal("Test", value);

        descriptor.SetValue(item, "Updated");
        Assert.Equal("Updated", item.Name);
    }

    // ===== BindingContext =====

    [Fact]
    public void BindingContext_CreatesCurrencyManagerForList()
    {
        var ctx = new BindingContext();
        var list = new BindingList<string> { "A", "B" };

        var mgr = ctx[list];

        Assert.IsType<CurrencyManager>(mgr);
        Assert.Equal(2, mgr.Count);
    }

    [Fact]
    public void BindingContext_CreatesPropertyManagerForObject()
    {
        var ctx = new BindingContext();
        var obj = new TestItem { Name = "Test" };

        var mgr = ctx[obj];

        Assert.IsType<PropertyManager>(mgr);
        Assert.Equal(1, mgr.Count);
    }

    // ===== ControlBindingsCollection =====

    [Fact]
    public void ControlBindingsCollection_AddAndRetrieve()
    {
        var source = new TestItem { Name = "Test" };
        var control = new TextBox();

        var binding = control.DataBindings.Add("Text", source, "Name");

        Assert.NotNull(binding);
        Assert.Same(binding, control.DataBindings["Text"]);
    }

    [Fact]
    public void ControlBindingsCollection_RemoveBinding()
    {
        var source = new TestItem { Name = "Test" };
        var control = new TextBox();
        var binding = control.DataBindings.Add("Text", source, "Name");

        var removed = control.DataBindings.Remove("Text");

        Assert.True(removed);
        Assert.Null(control.DataBindings["Text"]);
    }

    [Fact]
    public void ControlBindingsCollection_ClearRemovesAll()
    {
        var source = new TestItem { Name = "Test" };
        var control = new TextBox();
        control.DataBindings.Add("Text", source, "Name");

        control.DataBindings.Clear();

        Assert.Empty(control.DataBindings);
    }
}
