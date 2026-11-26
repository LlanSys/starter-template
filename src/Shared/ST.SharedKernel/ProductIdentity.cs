namespace ST.SharedKernel;

/// <summary>
/// The product's short technical prefix (for example <c>ST</c>), read from this assembly's root namespace.
/// rename-template.ps1 renames namespaces, so values built from this follow a rename automatically;
/// a hard-coded prefix string would not.
/// </summary>
public static class ProductIdentity
{
    public static readonly string Prefix = typeof(ProductIdentity).Namespace!.Split('.')[0];
}
