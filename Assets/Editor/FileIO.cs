using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class FileIO
{
    public static List<List<string>> ReadXLS(string filetoread)
    {
        // Must be saved as excel 2003 workbook, not 2007, mono issue really
        string con = "Driver={Microsoft Excel Driver (*.xls)}; DriverId=790; Dbq=" + filetoread + ";";
        Debug.Log(con);
        string yourQuery = "SELECT * FROM [Sheet1$]";
        // our odbc connector 
        OdbcConnection oCon = new OdbcConnection(con);
        // our command object 
        OdbcCommand oCmd = new OdbcCommand(yourQuery, oCon);
        // table to hold the data 
        DataTable dtYourData = new DataTable("YourData");
        // open the connection 
        oCon.Open();
        // lets use a datareader to fill that table! 
        OdbcDataReader rData = oCmd.ExecuteReader();
        // now lets blast that into the table by sheer man power! 
        dtYourData.Load(rData);
        // close that reader! 
        rData.Close();
        // close connection to the spreadsheet! 
        oCon.Close();

        if (dtYourData.Rows.Count <= 0)
        {
            Debug.LogWarning(filetoread + " is empty! Nothing has been imported!");
            return null;
        }

        if (dtYourData.Columns[0].ColumnName != "Key" || dtYourData.Columns[1].ColumnName != "Value")
        {
            Debug.LogError(filetoread + " is not correct in columns! Import has been stopped! Check the file.");
            return null;
        }

        var parsedXLS = new List<List<string>>();

        //Set the column
        var columnNameList = new List<string>();
        for (int col = 0; col < dtYourData.Columns.Count; col++)
        {
            columnNameList.Add(dtYourData.Columns[col].ColumnName);
            Debug.Log(dtYourData.Columns[col].ColumnName);
        }
        parsedXLS.Add(columnNameList);

        //Set the data
        for (int row = 0; row < dtYourData.Rows.Count; row++)
        {
            var tempList = new List<string>();
            //Debug.Log("Add key, value:");
            for (int col = 0; col < dtYourData.Columns.Count; col++)
            {
                tempList.Add(dtYourData.Rows[row][col].ToString());
                //Debug.Log(dtYourData.Rows[row][col].ToString());
            }
            //Debug.Log("from XLS.");
            parsedXLS.Add(tempList);
        }
        return parsedXLS;
    }

    public static void WriteXML(FileInfo fileinfo, List<List<string>> parsedXML,List<string> titles)
    {
        StreamWriter writer;

        if (!fileinfo.Exists)
        {
            writer = fileinfo.CreateText();
        }
        else
        {
            fileinfo.Delete();
            writer = fileinfo.CreateText();
        }

        //write data to file.
        {
            writer.Write("<Resources>\n");
            for (int i=0;i<parsedXML.Count;i++)
            {
                //Check title num and value num.
                if (parsedXML[i].Count != titles.Count)
                {
                    Debug.LogError("Not correct title and value num.");
                    return;
                }
                writer.Write("\t<" + titles[0] + " " + titles[1] + "=\"" + parsedXML[i][1] + "\"  " + titles[2] + "=\"" + parsedXML[i][2] + "\"  " + titles[3] + "=\"" + parsedXML[i][3] + "\">" + parsedXML[i][0] + "</" + titles[0] + ">\n");
            }
            writer.Write("</Resources>");
        }

        writer.Close();
        Debug.Log("File exported.");
    }
}
