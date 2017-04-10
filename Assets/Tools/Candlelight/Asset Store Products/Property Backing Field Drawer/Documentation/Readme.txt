Property Backing Field Drawer readme
================================================================================
Although alpha and beta releases of Unity are not officially supported, this
package will sometimes be updated during test cycles to add support as feasible.

PATCH RELEASES ARE NOT OFFICIALLY SUPPORTED. USE THEM AT YOUR OWN RISK.

More and up to date information available at
http://developers.candlelightinteractive.com/


Installing Property Backing Field Drawer
--------------------------------------------------------------------------------

Simply import the package and everything should "just work."

If you experience any problems, please open the Unity preferences menu, navigate
to the Candlelight section on the left, select the Property Backing Field tab,
and use the buttons at the bottom to report a bug or visit the support forum.

Please feel free to redistribute this package in your own asset store tools.


Using the Package
--------------------------------------------------------------------------------

This package contains code for a custom property drawer that allows you to
specify a serialized field on an Object is a backing field for some property.
The most basic application is to use a "m_" or "_" prefix on your property's
backing field in conjuction with PropertyBackingFieldAttribute:

    [SerializeField, Candlelight.PropertyBackingField]
    private int m_Int = 0;
    public int Int
    {
        get { return m_Int; }
        set { m_Int = value; }
    }

Your property must implement both a get and a set method, though they can have
different access modifiers (e.g., get may be public, and set may be private).

As long as the backing field type is assignable from the property type, they
need not match:

    [SerializeField, Candlelight.PropertyBackingField]
    private Component m_Anim = null;
    public Animation Anim
    {
        get { return m_Anim == null ? null : m_Anim as Animation; }
        set
        {
            if (m_Anim != value && value is Animation )
            {
                m_Anim = value;
            }
            else
            {
                m_Anim = null;
            }
        }
    }

For serializable IList properties (e.g., T[] and List<T>), you should
implement methods prefixed with "Get" and "Set," respectively:

    [SerializeField, Candlelight.PropertyBackingField]
    private int[] m_Arr = new int[1];
    public int[] GetArr()
    {
        return (int[])m_Arr.Clone();
    }
    public void SetArr(int[] value)
    {
        m_Arr = (int[])value.Clone();
    }

As with simple properties, the type of the backing field need not match the type
of the property methods. The backing field may be List<T> and the property T[]:

    [SerializeField, Candlelight.PropertyBackingField]
    private List<int> m_Lst = new List<int>(new int[1]);
    public int[] GetLst()
    {
        return m_Lst.ToArray();
    }
    public void SetLst(int[] value)
    {
        m_Lst = new List<int>(value);
    }

If you have a property drawer you would already like to use in conjunction with
this one, you simply need to specify the type assigned to the property drawer
in its CustomPropertyDrawer attribute as an additional argument. If the type is
a PropertyAttribute that takes arguments, you specify them as additional params:

    [SerializeField, Candlelight.PropertyBackingField(
        typeof(RangeAttribute), 0f, 1f // corresponds to [Range(0f, 1f)]
    )]
    private float m_Float;
    public float Float
    {
        get { return m_Float; }
        set { m_Float = value; }
    }

If you designate a custom, serializable class or struct as a backing field, it
must implement Candlelight.IPropertyBackingFieldCompatible so that the value of
its SerializedProperty representation can be retrieved without affecting the
object in the backing field, and so that the retrieved representations's
serialized values can be compared with those of the object in the backing field.

If your backing field does not have a "m_" or "_" prefix, or if its name
otherwise differs from the property to which it corresponds, the name of the
property can be supplied manually:

    [SerializeField, Candlelight.PropertyBackingField("SomeInt")]
    private int m_Int = 0;
    public int SomeInt
    {
        get { return m_Int; }
        set { m_Int = value; }
    }


Known Issues
--------------------------------------------------------------------------------

- A backing field may only be associated with one property or get/set pair.
- Due to a bug in Unity, if you set a property with its prefab value to a new
  value, undo the change, and then redo it, the field and any fields it affected
  will be dirtied, even though they now match their prefab values.
- If you set a property that dirties other serialized fields and then revert the
  property's value to its prefab value via the context menu, the other fields
  will remain dirty even if they now match their prefab values.
- All array properties will always trigger once upon the first undo.
- Due to how Unity's reorderable list inspectors work, moving an item in a
  reorderable list will trigger an IList setter twice even if it has a value
  check.
- Property setters for custom serializable classes will always supply a new
  instance (according to your implementation of System.ICloneable.Clone()).
  Consequently, changing any serialized field on the object via the inspector
  will submit a new instance to the setter, and any logic that triggers only on
  a reference change will always occur.
- Due to a bug in Unity, array sizes are not always set when using the context
  menu option to Set Value to X when multiple objects are selected.
- If you have a decorator drawer on a field that is marked as a property backing
  field, and which uses a default drawer for a Generic property type (i.e. a
  custom class or struct), the decorator will be drawn twice. The current
  implementation draws the second decorator over the first in order to minimize
  the effect.
- Quaternion backing fields using the default drawer will not display in the
  inspector. It is recommended you instead use the included EulerAnglesDrawer.
- If you override a property in a child class, the parent class implementation
  will be called. To work around this limitation, specify an override name for
  a private property in your PropertyBackingFieldAttribute constructor. This
  property should wrap the call to the virtual property, causing its
  implementation on the current type to be called. If you suffix your wrapper
  property/method with "_PBFEditorProperty" there is no need to manually specify
  the name of the wrapper property in your PropertyBackingFieldAttribute.