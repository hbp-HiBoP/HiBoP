using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HBP.Core.Data
{
    public sealed class LoadingRecoveryItem
    {
        public string Kind { get; }
        public string ID { get; }
        public ReadOnlyCollection<string> Reasons { get; }
        public BaseData QuarantinedObject { get; }

        internal LoadingRecoveryItem(string kind, string id, IEnumerable<string> reasons)
        {
            Kind = kind;
            ID = id ?? string.Empty;
            Reasons = new ReadOnlyCollection<string>((reasons ?? Enumerable.Empty<string>()).ToList());
            QuarantinedObject = null;
        }

        internal LoadingRecoveryItem(string kind, BaseData value, IEnumerable<string> reasons)
        {
            Kind = kind;
            ID = value?.ID ?? string.Empty;
            Reasons = new ReadOnlyCollection<string>((reasons ?? Enumerable.Empty<string>()).ToList());
            QuarantinedObject = value;
        }
    }

    public sealed class LoadingRecoveryReport
    {
        public static LoadingRecoveryReport Empty { get; } = new(Array.Empty<LoadingRecoveryItem>());

        public ReadOnlyCollection<LoadingRecoveryItem> Items { get; }
        public bool HasIssues => Items.Count > 0;

        internal LoadingRecoveryReport(IEnumerable<LoadingRecoveryItem> items)
        {
            Items = new ReadOnlyCollection<LoadingRecoveryItem>((items ?? Enumerable.Empty<LoadingRecoveryItem>()).ToList());
        }
    }
}
