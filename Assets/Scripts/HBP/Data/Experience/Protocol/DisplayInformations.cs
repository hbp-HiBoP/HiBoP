using UnityEngine;
using Tools.CSharp;
using System;

namespace HBP.Data.Experience.Protocol
{
    /**
    * \class DisplayInformations
    * \author Adrien Gannerie
    * \version 1.0
    * \date 05 janvier 2017
    * \brief Display informations of a Bloc.
    * 
    * \detail Class which define the display informations of a Bloc.
    *     - Name.
    *     - Row.
    *     - Column.
    *     - Illustration path.
    *     - Sort.
    *     - Window.
    *     - BaseLine.
    */
    public class DisplayInformations : ICloneable
    {
        #region Properties
        /// <summary>
        /// Name of the bloc.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Position of the bloc.
        /// </summary>
        public BlocPosition Position { get; set; }
        /// <summary>
        /// Illustration path of the bloc.
        /// </summary>
        public string IllustrationPath { get; set; }
        /// <summary>
        /// Sort lines of the bloc.
        /// </summary>
        public string Sort { get; set;}
        /// <summary>
        /// Window of the bloc (\a x : time before main event in ms. \a y : time after main event in ms.)
        /// </summary>
        public Window Window { get; set; }
        /// <summary>
        /// BaseLine of the bloc (\a x : start of the baseline in ms. \a y : end of the baseline in ms.)
        /// </summary>
        public Window BaseLine { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new display informations instance.
        /// </summary>
        /// <param name="position">Position of the bloc.</param>
        /// <param name="name">Name of the bloc.</param>
        /// <param name="illustrationPath">Illustration of the bloc path</param>
        /// <param name="sort">Sort of the bloc.</param>
        /// <param name="window">Window of the bloc.</param>
        /// <param name="baseLine">Baseline of the bloc.</param>
        public DisplayInformations(BlocPosition position,string name,string illustrationPath,string sort,Window window,Window baseLine)
        {
            Name = name;
            Position = position;
            IllustrationPath = illustrationPath;
            Sort = sort;
            Window = window;
            BaseLine = baseLine;
        }
        /// <summary>
        /// Create a new display informations instance.
        /// </summary>
        /// <param name="row">Row of the bloc.</param>
        /// <param name="col">Column of the bloc.</param>
        /// <param name="name">Name of the bloc.</param>
        /// <param name="illustrationPath">Illustration path of the bloc.</param>
        /// <param name="sort">Sort of the bloc.</param>
        /// <param name="window">Window of the bloc.</param>
        /// <param name="baseLine">BaseLine of the bloc.</param>
        public DisplayInformations(int row, int col, string name, string illustrationPath, string sort, Vector2 window, Vector2 baseLine)
        {
            Name = name;
            Position = new BlocPosition(row, col);
            IllustrationPath = illustrationPath;
            Sort = sort;
            Window = new Window(window);
            BaseLine = new Window(baseLine);
        }
        /// <summary>
        /// Create a new display informations by position.
        /// </summary>
        /// <param name="row">Row of the bloc.</param>
        /// <param name="col">Column of the bloc.</param>
        public DisplayInformations(int row, int col) : this(row, col, string.Empty, string.Empty, string.Empty, Vector2.zero, Vector2.zero)
        {
        }
        /// <summary>
        /// Create a new display informations with default values.
        /// </summary>
        public DisplayInformations() : this(0,0,string.Empty,string.Empty,string.Empty,Vector2.zero,Vector2.zero)
		{
        }
        #endregion

        #region Operators  
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public object Clone()
        {
            return new DisplayInformations(Position.Row, Position.Column, Name.Clone() as string, IllustrationPath.Clone() as string, Sort.Clone() as string, new Vector2(Window.Start,Window.End), new Vector2(BaseLine.Start,BaseLine.End));
        }
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            DisplayInformations p = obj as DisplayInformations;
            if (p == null)
            {
                return false;
            }
            else
            {
                return Name == p.Name && Position == p.Position && IllustrationPath == p.IllustrationPath && Sort == p.Sort && Window == p.Window && BaseLine == p.BaseLine;
            }
        }
        /// <summary>
        /// Get hash code.
        /// </summary>
        /// <returns>HashCode.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        /// <summary>
        /// Operator equals.
        /// </summary>
        /// <param name="a">Display informations to compare.</param>
        /// <param name="b">Display informations to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(DisplayInformations a, DisplayInformations b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }
        /// <summary>
        /// Operator not equals.
        /// </summary>
        /// <param name="a">Display informations to compare.</param>
        /// <param name="b">Display informations to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(DisplayInformations a, DisplayInformations b)
        {
            return !(a == b);
        }
        #endregion
    }
}