﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FrbaCrucero.DB;

namespace FrbaCrucero.Forms
{
    public class Alta : FormNavegable
    {
        protected static Dictionary<Type, Action<Control>> cleaners = new Dictionary<Type, Action<Control>>() { 
            {typeof(TextBox), c => ClearTextBox((TextBox)c)},
            {typeof(CheckBox), c => ((CheckBox)c).Checked = false},
            {typeof(ListBox), c =>{} },
            {typeof(RadioButton), c => ((RadioButton)c).Checked = false},
            {typeof(GroupBox), c => ClearControls(((GroupBox)c).Controls)},
            {typeof(Panel), c => ClearControls(((Panel)c).Controls)},
            {typeof(NumericUpDown), c => ((NumericUpDown)c).Value=0},
            {typeof(DomainUpDown), c => {}},
            {typeof(Button), c => {}},
            {typeof(Label), c => {}},
            {typeof(ComboBox), c => ClearCombo((ComboBox)c)},
            {typeof(CheckedListBox), c => ClearItems((CheckedListBox)c)},
            {typeof(DataGridView), c => ((DataGridView)c).DataSource=null},
    };

        public delegate FormNavegable ConstructorModificacion(int IdElemento);
        protected ConstructorModificacion constructorEdicion;

        public Alta(FormNavegable owner)
            : base(owner)
        {
        }

        public Alta(FormNavegable owner, ConstructorModificacion constructorEdicion)
            : base(owner)
        {
            this.constructorEdicion = constructorEdicion;
        }

        public Alta()
        {
        }

        public static void ClearCombo(ComboBox c)
        {
            if (c.Enabled)
                c.SelectedIndex = -1;
        }


        public static void ClearTextBox(TextBox c)
        {
            if (c.Enabled)
                c.Clear();
        }


        public static void ClearControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
                cleaners[control.GetType()].Invoke(control);
        }

        public virtual void Limpiar()
        {
            ClearControls(this.Controls);
        }

        public void ValidarNumericos(params string[] campos)
        {
            int i;
            foreach (string campo in campos)
                if ((!int.TryParse(campo, out i)) && (campo != null) && (campo != ""))
                    errorMessage += ("\"" + campo + "\" no es un número válido \n");
        }

        public void ValidarFechas(int dia, int mes, int anio)
        {
            try
            {
                new DateTime(anio, mes, dia);
            }
            catch (ArgumentOutOfRangeException)
            {
                errorMessage += "La fecha es inválida\n";
            }
        }

        public void ValidarVaciosYLongitud(string[] nombresCampos, object[] campos)
        {
            int i;
            for (i = 0; i < campos.Length; i++)
            {
                if (campos[i] == null)
                {
                    errorMessage += ("El campo " + nombresCampos[i] + " no ha sido completado\n");
                }
                else if (campos[i].GetType() == typeof(string))
                {
                    if (campos[i].ToString() == "")
                        errorMessage += ("El campo " + nombresCampos[i] + " no ha sido completado\n");
                    else if (campos[i].ToString().Length > 35)
                        errorMessage += ("El campo " + nombresCampos[i] + " es demasiado largo\n");
                }
            }
        }

        public void ValidarCollecionVacia<T>(string nombreCampo, List<T> campo)
        {
            if (campo.Count == 0)
                errorMessage += ("No se han seleccionado opciones para " + nombreCampo + "\n");
        }

        public bool HuboErrores()
        {

            ValidarErroresConcretos();
            return errorMessage != "";
        }

        public void ValidarErrores()
        {
            errorMessage = "";
            if (HuboErrores())
                throw new ExcepcionFrbaCrucero(errorMessage);

        }

        public virtual void ValidarErroresConcretos()
        {
        }

        public List<T> CheckListToList<T>(CheckedListBox checkedList)
        {
            List<T> lista = new List<T>();
            foreach (T elemento in checkedList.CheckedItems)
                lista.Add(elemento);
            return lista;
        }

        public List<T> CheckListToList<T>(CheckedListBox checkedList, List<T> anterior)
        {
            List<T> lista = new List<T>();
            foreach (T elemento in checkedList.CheckedItems)
                lista.Add(elemento);
            if (!(lista.All<T>(e => anterior.Contains(e)) && anterior.All<T>(e => lista.Contains(e))))
                checkedList.ClearSelected();
            return lista;
        }

        public String listar<T>(List<T> anterior, List<T> lista)
        {
            string s = "";
            s += "Anterior:\n";
            foreach (T e in anterior)
                s += e.ToString() + "\n";
            s += "Lista:\n";
            foreach (T e in lista)
                s += e.ToString() + "\n";
            return s;
        }

        public static void ClearItems(CheckedListBox checkedListBox)
        {
            object[] items = new object[checkedListBox.Items.Count];
            checkedListBox.Items.CopyTo(items, 0);
            checkedListBox.Items.Clear();
            checkedListBox.Items.AddRange(items);
        }

        public void cargarBotonModificarDatos(DataGridView grilla, string boton)
        {
            DataGridViewButtonColumn col = new DataGridViewButtonColumn();
            col.Text = boton;
            col.Name = "Operación";
            col.UseColumnTextForButtonValue = true;
            grilla.Columns.Add(col);
        }

        public void cargarGrilla(DataGridView grid, DataTable dataTable)
        {
            if (dataTable.Rows.Count.Equals(0))
            {
                dataTable = new DataTable();
                dataTable.Columns.Add("Lo Sentimos");
                dataTable.Rows.Add(" No hay datos que cumplan este criterio");
                if (grid.Columns.Contains("Operación"))
                    grid.Columns["Operación"].Visible = false;
            }
            else
                if (grid.Columns.Contains("Operación"))
                    grid.Columns["Operación"].Visible = true;
            grid.DataSource = dataTable;

            grid.AutoResizeColumns(); //ajusta el tamaño de las columnas y filas a su contenido
            grid.AutoResizeRows();
            grid.ReadOnly = true;
        }

        public void operacionConSeleccionado(DataGridView grilla, DataGridViewCellEventArgs e, Func<DataGridViewCellEventArgs, DataGridViewCellCollection, Boolean> condicion)
        {
            if (e != null)
            {
                try
                {
                    if (grilla[0, e.RowIndex].Value == null)
                    {

                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                }
                try
                {
                    DataGridViewCellCollection celdas = grilla.Rows[e.RowIndex].Cells;
                    if (condicion(e, celdas))
                    {
                        gridClickAction(celdas);
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
            else
            {
                try
                {
                    DataGridViewCellCollection celdas = grilla.Rows[grilla.SelectedRows[0].Index].Cells;
                    if (condicion(e, celdas))
                    {
                        gridClickAction(celdas);
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
        }

        public void operacionConSeleccionado(DataGridView grilla, DataGridViewCellEventArgs e)
        {
            this.operacionConSeleccionado(grilla, e, (ev, c) => true);
        }


        public void operacionBotonConSeleccionado(DataGridView grilla, DataGridViewCellEventArgs e)
        {
            operacionConSeleccionado(grilla, e, (ev, celdas) => ev.ColumnIndex == celdas["Operación"].ColumnIndex);
        }



        public virtual void gridClickAction(DataGridViewCellCollection celdas)
        {
            int aModificar = Convert.ToInt32(celdas["ID"].Value);
            constructorEdicion(aModificar).StandaloneOpen();
        }


    }
}
