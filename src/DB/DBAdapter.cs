﻿using System.Text.RegularExpressions;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Data;
using FrbaCrucero;

namespace FrbaCrucero.DB
{
    class DBAdapter
    {

            private static Dictionary<int, ExcepcionFrbaCrucero> excepciones = new Dictionary<int, ExcepcionFrbaCrucero>();

            public static Dictionary<int, ExcepcionFrbaCrucero> Excepciones
            {
                get
                {
                    return excepciones;
                }
                set
                {
                    excepciones = value;
                }
            }

            // Métodos privados

            private static void _ejecutarProcedure(string procedure, List<string> args, params object[] values)
            {
                SqlConnection conexion = new SqlConnection();
                SqlCommand comando = new SqlCommand();

                conexionSql(conexion, comando);
                comando.CommandType = CommandType.StoredProcedure;
                comando.CommandText = "EYE_OF_THE_TRIGGER." + procedure;
                if (_validateArgumentsAndParameters(args, values))
                {
                    _loadSqlCommand(args, values, comando);
                }
                comando.ExecuteNonQuery();

                if (conexion != null)
                {
                    conexion.Close();
                }

            }


            private static bool _checkIfExists(string procedure, List<string> args, params object[] values)
            {
                SqlConnection conexion = new SqlConnection();
                SqlCommand comando = new SqlCommand();
                SqlDataReader dataReader;
                DataTable dataTable = new DataTable();
                //TODO revisar que es lo que rompe aca y ver si conviene atajar la excepcion aca o en donde se use.
                try
                {
                    conexionSql(conexion, comando);
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.CommandText = "EYE_OF_THE_TRIGGER." + procedure;
                    if (_validateArgumentsAndParameters(args, values))
                    {
                        _loadSqlCommand(args, values, comando);
                    }
                    dataReader = comando.ExecuteReader();
                    return dataReader.HasRows;
                }
                catch (Exception excepcion)
                {
                    throw excepcion;
                }

                finally
                {
                    if (conexion != null)
                    {
                        conexion.Close();
                    }
                }
            }


            private static int _ejecutarProcedureWithReturnValue(string procedure, List<string> args, params object[] values)
            {
                SqlConnection conexion = new SqlConnection();
                SqlCommand comando = new SqlCommand();


                try
                {
                    conexionSql(conexion, comando);
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.CommandText = "EYE_OF_THE_TRIGGER." + procedure;
                    if (_validateArgumentsAndParameters(args, values))
                    {
                        _loadSqlCommand(args, values, comando);
                    }
                    comando.Parameters.Add("@RETURN_VALUE", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;
                    comando.ExecuteNonQuery();
                    return (int)comando.Parameters["@RETURN_VALUE"].Value;
                }
                catch (SqlException e)
                {
                    throw e;
                }
                finally
                {
                    if (conexion != null)
                    {
                        conexion.Close();
                    }
                }
            }

            private static string _ejecutarProcedureWithReturnString(string procedure, List<string> args, params object[] values)
            {

                DataTable stringEnTabla = _traerDataTable(procedure, args, values);
                DataRowCollection fila = stringEnTabla.Rows;
                DataRow primeraFila = fila[0];
                return primeraFila[0].ToString();


            }

            private static decimal _ejecutarProcedureWithReturnDecimal(string procedure, List<string> args, object[] values)
            {

                DataTable stringEnTabla = _traerDataTable(procedure, args, values);
                DataRowCollection fila = stringEnTabla.Rows;
                DataRow primeraFila = fila[0];
                string contenidoEnCadena = primeraFila[0].ToString();
                System.Diagnostics.Debug.WriteLine("El contenido de la cadena traída es: " + contenidoEnCadena);
                return Convert.ToDecimal(contenidoEnCadena);
            }

            private static DataTable _traerDataTable(string procedure, List<string> args, params object[] values)
            {
                SqlConnection conexion = new SqlConnection();
                SqlCommand comando = new SqlCommand();
                SqlDataReader dataReader;
                DataTable dataTable = new DataTable();

                try
                {
                    conexionSql(conexion, comando);
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.CommandText = "EYE_OF_THE_TRIGGER." + procedure;
                    if (_validateArgumentsAndParameters(args, values))
                    {
                        _loadSqlCommand(args, values, comando);
                    }
                    dataReader = comando.ExecuteReader();
                    dataTable.Load(dataReader);
                    return dataTable;
                }

                finally
                {
                    if (conexion != null)
                    {
                        conexion.Close();
                    }
                }
            }


            private static List<string> _generateArguments(string procedure)
            {
                SqlConnection conexion = new SqlConnection();
                SqlCommand comando = new SqlCommand();
                SqlDataReader dataReader;
                DataTable dataTable = new DataTable();
                List<string> args = new List<string>();
                try
                {
                    conexionSql(conexion, comando);
                    comando.CommandType = CommandType.Text;
                    comando.CommandText = "SELECT PARAMETER_NAME FROM information_schema.parameters WHERE SPECIFIC_SCHEMA='EYE_OF_THE_TRIGGER' AND SPECIFIC_NAME='" + procedure + "'";
                    dataReader = comando.ExecuteReader();
                    dataTable.Load(dataReader);
                    foreach (DataRow d in dataTable.Rows)
                    {
                        args.Add(d[0].ToString());
                    }
                    return args;
                }
                catch (Exception)
                {
                    return null;
                }

                finally
                {
                    if (conexion != null)
                    {
                        conexion.Close();
                    }
                }
            }


            private static bool _validateArgumentsAndParameters(List<string> args, params object[] values)
            {
                if (args != null && values != null)
                {
                    if (args.Count != values.Length)
                    {
                        throw new ApplicationException();
                    }
                    return true;
                }
                return false;
            }

            private static void _loadSqlCommand(List<string> args, object[] values, SqlCommand cm)
            {
                for (int i = 0; i < args.Count; i++)
                {
                    cm.Parameters.AddWithValue(args[i], values[i]);
                }
            }

            // Métodos públicos

            /// <summary>
            /// Ejecuta un SP y devuelve un datatable con el resultado del mismo.
            /// </summary>
            /// <param name="procedure">Nombre del SP almacenado en la DB sin el nombre del esquema delante.</param>
            /// <param name="values">Argumentos que recibe el SP.</param>
            public static DataTable traerDataTable(string procedure, params object[] values)
            {
                List<string> args = _generateArguments(procedure);
                return _traerDataTable(procedure, args, values);
            }

            /// <summary>
            /// Ejecuta un SP y devuelve un datatable con el resultado del mismo.
            /// </summary>
            /// <param name="procedure">Nombre del SP almacenado en la DB sin el nombre del esquema delante.</param>
            /// <returns></returns>
            public static DataTable traerDataTable(string procedure)
            {
                return _traerDataTable(procedure, null, null);
            }

            /// <summary>
            /// Ejecuta un SP.
            /// </summary>
            /// <param name="procedure">Nombre del SP almacenado en la DB sin el nombre del esquema delante.</param>
            /// <param name="values">Argumentos que recibe el SP.</param>
            public static void ejecutarProcedure(string procedure, params object[] values)
            {
                List<string> argumentos = _generateArguments(procedure);
                _ejecutarProcedure(procedure, argumentos, values);
            }

            /// <summary>
            /// Ejecuta un stored procedure.
            /// </summary>
            /// <param name="procedure">Nombre del SP almacenado en la DB sin el nombre del esquema delante.</param>
            public static void ejecutarProcedure(string procedure)
            {
                _ejecutarProcedure(procedure, null, null);
            }

            /// <summary>
            /// Ejecuta una consulta (a partir de un SP) y devuelve si encontró datos o no.
            /// </summary>
            /// <param name="procedure">Nombre del SP almacenado en la DB sin el nombre del esquema delante.</param>
            /// <param name="values">Argumentos que recibe el SP.</param>
            /// <returns> True: la consulta devolvió alguna fila. False: no devolvió filas.</returns>
            public static bool checkIfExists(string procedure, params object[] values)
            {
                List<string> argumentos = _generateArguments(procedure);
                return _checkIfExists(procedure, argumentos, values);
            }

            /// <summary>
            /// Ejecuta una consulta (a partir de un SP) y devuelve si encontró datos o no.
            /// </summary>
            /// <param name="procedure">Nombre del SP almacenado en la DB sin el nombre del esquema delante.</param>
            /// <returns> True: la consulta devolvió alguna fila. False: no devolvió filas.</returns>
            public static bool checkIfExists(string procedure)
            {
                return _checkIfExists(procedure, null, null);
            }

            /// <summary>
            /// Ejecuta un SP que devuelve un valor númerico y retorna dicho valor.
            /// </summary>
            /// <param name="procedure">Nombre del SP almacenado en la DB sin el nombre del esquema delante.</param>
            /// <param name="values">Argumentos que recibe el SP.</param>
            /// <returns> Valor de retorno del SP.</returns>
            public static int ejecutarProcedureWithReturnValue(string procedure, params object[] values)
            {
                List<string> argumentos = _generateArguments(procedure);
                return _ejecutarProcedureWithReturnValue(procedure, argumentos, values);
            }

            public static string ejecutarProcedureWithReturnString(string procedure, params object[] values)
            {
                List<string> argumentos = _generateArguments(procedure);
                string retorno = _ejecutarProcedureWithReturnString(procedure, argumentos, values);
                return retorno;
            }

            public static decimal ejecutarProcedureWithReturnDecimal(string procedure, params object[] values)
            {
                List<string> argumentos = _generateArguments(procedure);
                return _ejecutarProcedureWithReturnDecimal(procedure, argumentos, values);
            }

            public static void conexionSql(SqlConnection conexion, SqlCommand cm)
            {
                try
                {
                    string sconcompleto = ConfigurationManager.ConnectionStrings["db"].ConnectionString;
                    conexion.ConnectionString = sconcompleto;
                    cm.Connection = conexion;
                    conexion.Open();
                }
                catch (Exception excepcion)
                {
                    throw excepcion;
                }
            }

            public static void insertarDatosEnTabla(string tabla, params object[] valores)
            {

                int error = 0;
                error = ejecutarProcedureWithReturnValue("insertar_" + tabla, valores);
                CheckExcepcionPara(error);
            }

            public static void borrarDatosEnTabla(string tabla, params object[] valores)
            {

                int error = 0;
                error = ejecutarProcedureWithReturnValue("borrar_" + tabla, valores);
                CheckExcepcionPara(error);
            }

            public static void actualizarDatosEnTabla(string tabla, params object[] valores)
            {

                int error = 0;
                error = ejecutarProcedureWithReturnValue("actualizar_" + tabla, valores);
                CheckExcepcionPara(error);
            }


            public static void CheckExcepcionPara(int codigo)
            {
                if (codigo != 0)
                    throw ExcepcionPara(codigo);
            }

            public static ExcepcionFrbaCrucero ExcepcionPara(int codigo)
            {
                ExcepcionFrbaCrucero excepcion;
                excepciones.TryGetValue(codigo, out excepcion);
                return excepcion;
            }

            public static string[] dataTableToString(string procedure, string columna, params object[] argumentos)
            {
                DataTable elementos = traerDataTable(procedure, argumentos);
                List<string> lista = new List<string>();

                foreach (DataRow elemento in elementos.Rows)
                {
                    lista.Add(elemento[columna].ToString());
                }
                return lista.ToArray();
            }

        }
}