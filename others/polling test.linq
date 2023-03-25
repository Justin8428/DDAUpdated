<Query Kind="Statements" />

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

Example.Main();

public class Example
{
   public static void Main()
   {
      Task t = Task.Factory.StartNew( async () => {
                                  // while loop to poll cursor location
                                  while (true)
                                  {
								  	Screen s = Screen.FromPoint(Cursor.Position); // get the screen which contains the cursor
									Console.WriteLine(s.DeviceName.ToString());
									// 1000ms polling
									//Thread.Sleep(1000);
									await Task.Delay(1000); // more memory efficient delay https://stackoverflow.com/questions/23340894/polling-the-right-way // poll every 1s
								  }

                               } );
	  t.Wait(); // asynchrously poll the task
   }
}

