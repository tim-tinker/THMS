using System.ComponentModel;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class SortableBindingList<T> : BindingList<T>
    {
        private readonly Func<PropertyDescriptor, ListSortDirection, IComparer<T>> _createComparer;
        private bool _sorted;
        private PropertyDescriptor? _property;
        private ListSortDirection _direction;

        public SortableBindingList(
            IList<T> list,
            Func<PropertyDescriptor, ListSortDirection, IComparer<T>> createComparer)
            : base(list)
        {
            _createComparer = createComparer;
        }

        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => _sorted;
        protected override PropertyDescriptor? SortPropertyCore => _property;
        protected override ListSortDirection SortDirectionCore => _direction;

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            var comparer = _createComparer(prop, direction);
            if (Items is List<T> list)
            {
                list.Sort(comparer);
            }
            else
            {
                var sorted = Items.ToList();
                sorted.Sort(comparer);
                Items.Clear();
                foreach (var item in sorted)
                    Items.Add(item);
            }

            _property = prop;
            _direction = direction;
            _sorted = true;
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        protected override void RemoveSortCore()
        {
            _sorted = false;
            _property = null;
        }
    }
}
