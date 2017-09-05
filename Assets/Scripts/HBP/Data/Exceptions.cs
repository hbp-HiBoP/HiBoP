using System;

[Serializable]
public class HBPException : Exception
{
    public virtual string Title { get; protected set; }
    public override string Message
    {
        get
        {
            return base.Message;
        }
    }
    public override string ToString()
    {
        return Title;
    }
    public HBPException() { }
    public HBPException(string message) : base(message) { }
    public HBPException(string message, Exception inner) : base(message, inner) { }
    protected HBPException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class CannotFindDataInfoException : HBPException
{
    public CannotFindDataInfoException() { }
    public CannotFindDataInfoException(string patient, string column): base ("DataInfo needed for <color=red>"+ patient + "</color> in <color=red>" + column + "</color> column not found.\n\nPlease verify your dataset.")
    {
        Title ="DataInfo not found";
    }
    public CannotFindDataInfoException(string message, Exception inner) : base(message, inner) { }
    protected CannotFindDataInfoException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class DirectoryNotFoundException : HBPException
{
    public DirectoryNotFoundException() { }
    public DirectoryNotFoundException(string directory) : base("Directory <color=red>"+directory+"</color> not found.\n\nPlease verify the path.")
    { 
        Title = "Directory not found.";
    }
    public DirectoryNotFoundException(string message, Exception inner) : base(message, inner) { }
    protected DirectoryNotFoundException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class DirectoryNotProjectException : HBPException
{
    public DirectoryNotProjectException() { }
    public DirectoryNotProjectException(string directory) : base("Directory <color=red>"+ directory + "</color> is not a project.\n\nPlease verify your directory.")
    {
        Title = "Directory is not a project.";
    }
    public DirectoryNotProjectException(string directory, Exception inner) : base(directory, inner) { }
    protected DirectoryNotProjectException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class CanNotReadSettingsFileException : HBPException
{
    public CanNotReadSettingsFileException() { }
    public CanNotReadSettingsFileException(string file) : base("Settings <color=red>" + file + "</color> could not be loaded.\n\nPlease verify the file.")
    {
        Title = "Can not read settings file.";
    }
    public CanNotReadSettingsFileException(string file, Exception inner) : base(file, inner) { }
    protected CanNotReadSettingsFileException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class CanNotReadPatientFileException : HBPException
{
    public CanNotReadPatientFileException() { }
    public CanNotReadPatientFileException(string file) : base("Patient <color=red>" + file + "</color> could not be loaded.\n\nPlease verify the file.")
    {
        Title = "Can not read patient file.";
    }
    public CanNotReadPatientFileException(string message, Exception inner) : base(message, inner) { }
    protected CanNotReadPatientFileException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class CanNotReadGroupFileException : HBPException
{
    public CanNotReadGroupFileException() { }
    public CanNotReadGroupFileException(string file) : base("Group <color=red>" + file + "</color> could not be loaded.\n\nPlease verify the file.")
    {
        Title = "Can not read group file.";
    }
    public CanNotReadGroupFileException(string message, Exception inner) : base(message, inner) { }
    protected CanNotReadGroupFileException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class CanNotReadProtocolFileException : HBPException
{
    public CanNotReadProtocolFileException() { }
    public CanNotReadProtocolFileException(string file) : base("Protocol <color=red>" + file + "</color> could not be loaded.\n\nPlease verify the file.")
    {
        Title = "Can not read protocol file.";
    }
    public CanNotReadProtocolFileException(string message, Exception inner) : base(message, inner) { }
    protected CanNotReadProtocolFileException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class CanNotReadDatasetFileException : HBPException
{
    public CanNotReadDatasetFileException() { }
    public CanNotReadDatasetFileException(string file) : base("Dataset <color=red>" + file + "</color> could not be loaded.\n\nPlease verify the file.")
    {
        Title = "Can not read dataset file.";
    }
    public CanNotReadDatasetFileException(string message, Exception inner) : base(message, inner) { }
    protected CanNotReadDatasetFileException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class CanNotReadVisualizationFileException : HBPException
{
    public CanNotReadVisualizationFileException() { }
    public CanNotReadVisualizationFileException(string file) : base("Visualization <color=red>" + file + "</color> could not be loaded.\n\nPlease verify the file.")
    {
        Title = "Can not read visualization file.";
    }
    public CanNotReadVisualizationFileException(string message, Exception inner) : base(message, inner) { }
    protected CanNotReadVisualizationFileException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class CanNotSaveSettingsException : HBPException
{
    public CanNotSaveSettingsException() : base("Settings could not be saved.\n\nPlease verify your right.")
    {
        Title = "Can not save settings.";
    }
    public CanNotSaveSettingsException(string message, Exception inner) : base(message, inner) { }
    protected CanNotSaveSettingsException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class CanNotSavePatientException : HBPException
{
    public CanNotSavePatientException() { }
    public CanNotSavePatientException(string patient) : base(patient +" could not be saved.\n\nPlease verify your right.")
    {
        Title = "Can not save patient.";
    }
    public CanNotSavePatientException(string message, Exception inner) : base(message, inner) { }
    protected CanNotSavePatientException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class CanNotSaveGroupException : HBPException
{
    public CanNotSaveGroupException() { }
    public CanNotSaveGroupException(string group) : base(group + " could not be saved.\n\nPlease verify your right.")
    {
        Title = "Can not save group.";
    }
    public CanNotSaveGroupException(string message, Exception inner) : base(message, inner) { }
    protected CanNotSaveGroupException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class CanNotSaveProtocolException : HBPException
{
    public CanNotSaveProtocolException() { }
    public CanNotSaveProtocolException(string protocol) : base(protocol + " could not be saved.\n\nPlease verify your right.")
    {
        Title = "Can not save protocol.";
    }
    public CanNotSaveProtocolException(string message, Exception inner) : base(message, inner) { }
    protected CanNotSaveProtocolException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class CanNotSaveDatasetException : HBPException
{
    public CanNotSaveDatasetException() { }
    public CanNotSaveDatasetException(string dataset) : base(dataset + " could not be saved.\n\nPlease verify your right.")
    {
        Title = "Can not save dataset.";
    }
    public CanNotSaveDatasetException(string message, Exception inner) : base(message, inner) { }
    protected CanNotSaveDatasetException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class CanNotSaveVisualizationException : HBPException
{
    public CanNotSaveVisualizationException() { }
    public CanNotSaveVisualizationException(string visualization) : base(visualization + " could not be saved.\n\nPlease verify your right.")
    {
        Title = "Can not save visualization.";
    }
    public CanNotSaveVisualizationException(string message, Exception inner) : base(message, inner) { }
    protected CanNotSaveVisualizationException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class SettingsFileNotFoundException : HBPException
{
    public SettingsFileNotFoundException() : base("Settings file not found.\n\nPlease verify your project directory.")
    {
        Title = "Settings file not found.";
    }
    public SettingsFileNotFoundException(string message, Exception inner) : base(message, inner) { }
    protected SettingsFileNotFoundException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class MultipleSettingsFilesFoundException : HBPException
{
    public MultipleSettingsFilesFoundException() { }
    public MultipleSettingsFilesFoundException(string message) : base("More than one settings file found.\n\nPlease verify your project directory.")
    {
        Title = "Multiple settings files found.";
    }
    public MultipleSettingsFilesFoundException(string message, Exception inner) : base(message, inner) { }
    protected MultipleSettingsFilesFoundException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class CanNotDeleteOldProjectDirectory : HBPException
{
    public CanNotDeleteOldProjectDirectory() { }
    public CanNotDeleteOldProjectDirectory(string path) : base(path + " could not be deleted.\n\nPlease verify your right.")
    {
        Title = "Can not delete old project directory.";
    }
    public CanNotDeleteOldProjectDirectory(string message, Exception inner) : base(message, inner) { }
    protected CanNotDeleteOldProjectDirectory(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class CanNotRenameProjectDirectory : HBPException
{
    public CanNotRenameProjectDirectory() : base("Can not rename project directory.\n\nPlease verify your right.")
    {
        Title = "Can not rename project directory.";
    }
    public CanNotRenameProjectDirectory(string message, Exception inner) : base(message, inner) { }
    protected CanNotRenameProjectDirectory(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class ModeAccessException : HBPException
{
    public ModeAccessException() { }
    public ModeAccessException(string name) : base("No access for mode " + name)
    {
        Title = "Can not perform this operation.";
    }
    public ModeAccessException(string message, Exception inner) : base(message, inner) { }
    protected ModeAccessException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class EmptyFilePathException : HBPException
{
    public EmptyFilePathException() { }
    public EmptyFilePathException(string type) : base("The path of the file of type " + type + " is empty.\n\nPlease verify the path.")
    {
        Title = "Path of a file is empty";
    }
    public EmptyFilePathException(string message, Exception inner) : base(message, inner) { }
    protected EmptyFilePathException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class CanNotLoadTransformation : HBPException
{
    public CanNotLoadTransformation() { }
    public CanNotLoadTransformation(string path) : base(path + " could not be loaded.\n\nPlease verify the path or the file.")
    {
        Title = "Can not load transformation.";
    }
    public CanNotLoadTransformation(string message, Exception inner) : base(message, inner) { }
    protected CanNotLoadTransformation(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class CanNotLoadGIIFile : HBPException
{
    public CanNotLoadGIIFile() { }
    public CanNotLoadGIIFile(bool left, bool right) : base("Left: " + (left?"OK":"NOT OK") + "\n" + "Right: " + (right?"OK":"NOT OK"))
    {
        Title = "Can not load GII file.";
    }
    public CanNotLoadGIIFile(bool both) : base("Single: " + (both ? "OK" : "NOT OK"))
    {
        Title = "Can not load GII file.";
    }
    public CanNotLoadGIIFile(string message, Exception inner) : base(message, inner) { }
    protected CanNotLoadGIIFile(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class CanNotLoadNIIFile : HBPException
{
    public CanNotLoadNIIFile() { }
    public CanNotLoadNIIFile(string path) : base(path + " could not be loaded.\n\nPlease verify the file.")
    {
        Title = "Can not load NII file.";
    }
    public CanNotLoadNIIFile(string message, Exception inner) : base(message, inner) { }
    protected CanNotLoadNIIFile(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}