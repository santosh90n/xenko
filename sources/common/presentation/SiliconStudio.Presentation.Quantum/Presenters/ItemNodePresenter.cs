﻿using System;
using System.Collections.Generic;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Extensions;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Quantum;

namespace SiliconStudio.Presentation.Quantum.Presenters
{
    public class ItemNodePresenter : NodePresenterBase
    {
        protected readonly IObjectNode Container;

        public ItemNodePresenter([NotNull] INodePresenterFactoryInternal factory, IPropertyProviderViewModel propertyProvider, [NotNull] INodePresenter parent, [NotNull] IObjectNode container, Index index)
            : base(factory, propertyProvider, parent)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (container == null) throw new ArgumentNullException(nameof(container));
            Container = container;
            Descriptor = TypeDescriptorFactory.Default.Find(container.Descriptor.GetInnerCollectionType());
            OwnerCollection = parent;
            Type = (container.Descriptor as CollectionDescriptor)?.ElementType ?? (container.Descriptor as DictionaryDescriptor)?.ValueType;
            Index = index;
            Name = index.ToString();
            CombineKey = Name;
            DisplayName = Index.IsInt ? "Item " + Index : Index.ToString();

            container.ItemChanging += OnItemChanging;
            container.ItemChanged += OnItemChanged;
            AttachCommands();
        }

        public override void Dispose()
        {
            Container.ItemChanging -= OnItemChanging;
            Container.ItemChanged -= OnItemChanged;
        }

        public sealed override List<INodePresenterCommand> Commands { get; } = new List<INodePresenterCommand>();

        public INodePresenter OwnerCollection { get; }

        public sealed override Index Index { get; }

        public override Type Type { get; }

        public override bool IsEnumerable => Container.IsEnumerable;

        public override ITypeDescriptor Descriptor { get; }

        public override object Value => Container.Retrieve(Index);

        protected override IObjectNode ParentingNode => Container.ItemReferences != null ? Container.IndexedTarget(Index) : null;

        public override void UpdateValue(object newValue)
        {
            try
            {
                Container.Update(newValue, Index);
                Refresh();
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while updating the value of the node, see the inner exception for more information.", e);
            }
        }

        public override void AddItem(object value)
        {
            if (Container.IndexedTarget(Index) == null || !Container.IndexedTarget(Index).IsEnumerable)
                throw new NodePresenterException($"{nameof(MemberNodePresenter)}.{nameof(AddItem)} cannot be invoked on members that are not collection.");

            try
            {
                RaiseValueChanging(Value);
                Container.IndexedTarget(Index).Add(value);
                Refresh();
                RaiseValueChanged(Value);
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while adding an item to the node, see the inner exception for more information.", e);
            }
        }

        public override void AddItem(object value, Index index)
        {
            if (Container.IndexedTarget(Index) == null || !Container.IndexedTarget(Index).IsEnumerable)
                throw new NodePresenterException($"{nameof(MemberNodePresenter)}.{nameof(AddItem)} cannot be invoked on members that are not collection.");

            try
            {
                RaiseValueChanging(Value);
                Container.IndexedTarget(Index).Add(value, index);
                Refresh();
                RaiseValueChanged(Value);
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while adding an item to the node, see the inner exception for more information.", e);
            }
        }

        public override void RemoveItem(object value, Index index)
        {
            if (Container.IndexedTarget(Index) == null || !Container.IndexedTarget(Index).IsEnumerable)
                throw new NodePresenterException($"{nameof(MemberNodePresenter)}.{nameof(AddItem)} cannot be invoked on members that are not collection.");

            try
            {
                RaiseValueChanging(Value);
                Container.IndexedTarget(Index).Remove(value, index);
                Refresh();
                RaiseValueChanged(Value);
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while removing an item to the node, see the inner exception for more information.", e);
            }
        }

        public override NodeAccessor GetNodeAccessor()
        {
            return new NodeAccessor(Container, Index);
        }

        private void OnItemChanging(object sender, ItemChangeEventArgs e)
        {
            if (IsValidChange(e))
                RaiseValueChanging(e.NewValue);
        }

        private void OnItemChanged(object sender, ItemChangeEventArgs e)
        {
            if (IsValidChange(e))
            {
                Refresh();
                RaiseValueChanged(e.OldValue);
            }
        }

        private bool IsValidChange([NotNull] INodeChangeEventArgs e)
        {
            return IsValidChange(e.ChangeType, e.Index, Index);
        }

        public static bool IsValidChange(ContentChangeType changeType, Index changeIndex, Index presenterIndex)
        {
            // We should care only if the change is an update at the same index.
            // Other scenarios (add, remove) are handled by the parent node.
            switch (changeType)
            {
                case ContentChangeType.CollectionUpdate:
                    return Equals(changeIndex, presenterIndex);
                case ContentChangeType.CollectionAdd:
                case ContentChangeType.CollectionRemove:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
