#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace eztrans_server {
  public class ViewModelBase : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// It should be called only in the property setter.
    /// </summary>
    protected void Set<T>(ref T member, T value, [CallerMemberName] string? name = null) {
      if (Equals(member, value)) {
        return;
      }
      member = value;

      if (name != null) {
        NotifyChange(name);
      }
    }

    protected void NotifyChange(string name) {
      var ev = new PropertyChangedEventArgs(name);
      PropertyChanged?.Invoke(this, ev);
    }
  }


  public class RelayCommand : ICommand {
    readonly Action _Execute;
    readonly Func<bool> _CanExecute;

    public RelayCommand(Action execute) : this(execute, null) {
    }

    public RelayCommand(Action execute, Func<bool>? canExecute) {
      _Execute = execute;
      _CanExecute = canExecute ?? (() => true);
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object parameter) {
      return _CanExecute();
    }

    public void Execute(object parameter) {
      _Execute();
    }
  }

  public class RelayCommand<T> : ICommand where T : class {

    readonly Action<T?> _Execute;
    readonly Predicate<T?> _CanExecute;

    /// <summary>
    /// Creates a new command.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="canExecute">The execution status logic.</param>
    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null) {
      _Execute = execute;
      _CanExecute = canExecute ?? (_ => true);
    }

    #region ICommand Members

    ///<summary>
    ///Occurs when changes occur that affect whether or not the command should execute.
    ///</summary>
    public event EventHandler? CanExecuteChanged;

    ///<summary>
    ///Defines the method that determines whether the command can execute in its current state.
    ///</summary>
    ///<param name="parameter">Data used by the command. If the command does not require
    ///data to be passed, this object can be set to null.</param>
    ///<returns>
    ///true if this command can be executed; otherwise, false.
    ///</returns>
    public bool CanExecute(object parameter) {
      return _CanExecute(NullToDefault(parameter));
    }

    ///<summary>
    ///Defines the method to be called when the command is invoked.
    ///</summary>
    ///<param name="parameter">Data used by the command. If the command does not
    ///require data to be passed, this object can be set to <see langword="null" />.</param>
    public void Execute(object parameter) {
      _Execute(NullToDefault(parameter));
    }

    #endregion

    private static T? NullToDefault(object? parameter) {
      return parameter == null ? default : (T)parameter;
    }
  }
}
