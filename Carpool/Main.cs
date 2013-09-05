/* Carpool
 * Copyright 2013, Will Brown
 * See LICENSE for more information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Mono.Options;

namespace Carpool
{
	class Person
	{
		public Person(uint id, string name, char code)
		{
			Id = id;
			Name = name;
			Code = code;
		}

		public char Code { get; private set; }

		public uint Id { get; private set; }

		public string Name { get; private set; }

		public override string ToString()
		{
			return Name;
		}
	}

	class Ride
	{
		public Ride(Person driver, IEnumerable<Person> riders)
		{
			Driver = driver;
			this.riders = new List<Person>(riders);
		}

		public Person Driver { get; private set; }

		public IEnumerable<Person> Riders
		{
			get { return riders; }
		}

		private List<Person> riders;
	}

	class Disparity
	{
		public Disparity(Person driver, Person rider, uint magnitude)
		{
			Driver = driver;
			Rider = rider;
			Magnitude = magnitude;
		}

		public Person Driver { get; private set; }

		public Person Rider { get; private set; }

		public uint Magnitude { get; private set; }

		public override string ToString()
		{
			return string.Format("{0} owes {1} {2} ride{3}", Rider, Driver, Magnitude, Magnitude == 1 ? "" : "s");
		}
	}

	class Subset
	{
		public Subset(IEnumerable<Person> participants)
		{
			participantDrives = new Dictionary<Person,int>();
			foreach (var participant in participants)
				participantDrives.Add(participant, 0);
		}

		public static string GetKey(IEnumerable<Person> participants)
		{
			return String.Join("-", participants.OrderBy(x => x.Id).Select(x => x.Id));
		}

		public void AddRide(Person driver)
		{
			Trace.Assert(participantDrives.ContainsKey(driver));
			participantDrives [driver]++;
		}

		public IList<Person> Participants
		{
			get { return participantDrives.Keys.ToList(); }
		}

		public IDictionary<Person,int> Drives
		{
			get { return participantDrives; }
		}

		public string Key
		{
			get { return GetKey(Participants); }
		}

		private Dictionary<Person,int> participantDrives;
	}

	enum SelectionMethod
	{
		Pairs,
		Subsets,
		Points,
		Units
	}

	class MainClass
	{
		private static bool verbose = false;

		public static void Main(string[] args)
		{
			Console.WriteLine("Carpool v2");

			SelectionMethod method = SelectionMethod.Units;
			bool showHelp = args.Length == 0;
			var options = new OptionSet() {
                { "v|verbose", "Print details of method.", x => verbose = true },
                { "m=|method=", "Specify the method used to select a driver.",  (SelectionMethod x) => method = x },
                { "h|help|?", "Show this help.", x => showHelp = true }
            };
			List<string> anonArgs; 
			try
			{ 
				anonArgs = options.Parse(args);

				if (showHelp)
				{
					Console.WriteLine("Usage: Carpool [Options] <Filename> [Participants]\n" +
						"  Participants are the letter codes of the people who will show up for\n" +
						"  the car pool. If particpants are not provided, selection is based on\n" +
						"  everyone showing up."
					);
					Console.WriteLine("\nOptions:");
					using (var writer = new StreamWriter(Console.OpenStandardOutput()))
					{
						options.WriteOptionDescriptions(writer);
						writer.Flush();
					}
					Console.WriteLine("\nMethods:\n" +
						"  Units (Fair) - The Fagin/Williams method. (default)\n" +
						"  Subsets (Fair) - The brute force method.\n" +
						"  Pairs (Unfair) - The Carpool v1 method.\n" +
						"  Points (Unfair) - The Rafal method.\n"
					);
					Console.WriteLine();
					return;
				}

				if (anonArgs.Count < 1)
					throw new Exception("No input file specified.");
				if (anonArgs.Count > 2)
					throw new Exception("Unexpected arguments: " + String.Join(" ", anonArgs.Skip(2)));
			} catch (Exception e)
			{
				Console.WriteLine("Input arguments error: " + e.Message);
				return;
			}
			Console.WriteLine();

			try
			{
				var inputFile = anonArgs.First();
				var lines = File.ReadAllLines(inputFile);

				string peopleHereLine = null;
				if (anonArgs.Count > 1)
					peopleHereLine = anonArgs [1];

				var space = new char[] { ' ', '\t' };
				var riderData = lines.First().Split(space, StringSplitOptions.RemoveEmptyEntries);
				var rideData = lines.Skip(1)
    				.Where(x => !String.IsNullOrWhiteSpace(x))
    				.Select(line => line.Split(space).First()).ToArray();

				var people = new List<Person>(riderData.Length / 2);
				for (uint i = 0; i < riderData.Length; i += 2)
				{
					people.Add(new Person(i / 2, riderData [i + 1], char.Parse(riderData [i])));
				}

				var rides = rideData
    				.Select(codes => 
    					new Ride(people.Single(p => p.Code == codes.First()), 
    				             codes.Skip(1).Select(c => people.Single(p => p.Code == c)))
				).ToArray();

				List<Person> peopleHere = peopleHereLine == null ? 
                    people :
                    peopleHereLine.Select(c => people.Single(p => p.Code.ToString() == c.ToString().ToUpper())).ToList();

				PrintSelections(
                    FilterSelections(
                            ComputeSelections(method, people, rides, peopleHere)
                        , peopleHere)
                    , people.Count);
			} catch (FileNotFoundException e)
			{
				Console.WriteLine("File '" + e.FileName + "' does not exist.");
			} catch (Exception)
			{
				Console.WriteLine("Unexpected exception.");
				throw;
			}
		}

		public static IEnumerable<IEnumerable<Person>> ComputeSelections(SelectionMethod method, IList<Person> people, IList<Ride> rides, IList<Person> peopleHere)
		{
			Console.WriteLine("Using method: " + method);
			Console.WriteLine();

			switch (method)
			{
				case SelectionMethod.Pairs:
					return PairsMethod(people, rides, peopleHere);
				case SelectionMethod.Points:
					return PointsMethod(people, rides);
				case SelectionMethod.Units:
					return UnitsMethod(people, rides);
				case SelectionMethod.Subsets:
					return SubsetsMethod(people, rides, peopleHere);
				default:
					throw new InvalidOperationException();
			}
		}

		public static void PrintSelections(IEnumerable<IEnumerable<Person>> selections, int peopleCount)
		{
			if (selections == null || selections.Count() == 0 || selections.First().Count() == 0 || selections.First().Count() == peopleCount)
				Console.WriteLine("All equal. Anyone can drive!");
			else
			{
				bool first = true;
				foreach (var nextGroup in selections)
				{
					Console.Write("\t");
					if (selections.Count() > 1)
						Console.Write(first ? "First " : "Then ");
					Console.WriteLine(String.Join(" or ", nextGroup));
					first = false;
				}
			}

			Console.WriteLine();
		}

		// Fair
		// The Fagin/Williams Method 
		public static IEnumerable<IEnumerable<Person>> UnitsMethod(IList<Person> people, IList<Ride> rides)
		{
			int m = people.Count; // possible participants
			int U = MathEx.RangeLcm(1, m); // max units

			if (verbose) 
				Console.WriteLine("m={0}\nU={1}\n\n", m, U);

			Console.Write("PERSON->".PadLeft(10));
			foreach (var person in people)
				Console.Write(person.ToString().PadLeft(10));
			Console.WriteLine();

			var totalScores = new int[m];
			foreach (var ride in rides)
			{
				var scores = new int[m];
				int k = ride.Riders.Count() + 1; // ride participants
				scores [ride.Driver.Id] += U * (k - 1) / k;
				foreach (var rider in ride.Riders)
					scores [rider.Id] -= U / k;

				Debug.Assert(scores.Sum() == 0);
				for (int i = 0; i < m; i++)
					totalScores [i] += scores [i];

				if (verbose)
				{
					Console.Write(("U/k=" + (U / k)).PadLeft(10));
					foreach (var person in people)
						Console.Write(scores [person.Id].ToString().PadLeft(10));
					Console.WriteLine();
				}
			}

			Debug.Assert(totalScores.Sum() == 0);
			if (verbose)
				Console.WriteLine("Final Scores:");
			Console.Write("".PadLeft(10));
			foreach (var person in people)
				Console.Write(totalScores [person.Id].ToString().PadLeft(10));
			Console.WriteLine();

			return GetSelectionsFromScores(
                Enumerable.Range(0, totalScores.Length).ToDictionary(i => people.Single(x => x.Id == i), i => totalScores [i]));
		}

		static IEnumerable<IEnumerable<Person>> FilterSelections(IEnumerable<IEnumerable<Person>> selections, List<Person> peopleHere)
		{
			if (selections == null)
				return null;

			Console.WriteLine();
			Console.WriteLine("Given " + String.Join(", ", peopleHere.Select(p => p.Name)) + " show up...");

			return selections.Select(x => x.Where(p => peopleHere.Contains(p))).Where(g => g.Count() > 0);
		}

		public static IEnumerable<IEnumerable<Person>> GetSelectionsFromScores(IDictionary<Person,int> scores)
		{
			if (scores.Values.All(x => x == scores.First().Value))
				return Enumerable.Empty<IEnumerable<Person>>();

			return scores
                .GroupBy(p => p.Value)
                .OrderBy(p => p.Key)
                .Select(g => g.Select(r => r.Key));
		}

		// Fair (but does not commute rides between subsets)
		// Points within all possible combinations of people
		public static IEnumerable<IEnumerable<Person>> SubsetsMethod(IList<Person> people, IList<Ride> rides, IList<Person> peopleHere)
		{
			var subsets = new Dictionary<string,Subset>();
			foreach (var ride in rides)
			{
				var participants = ride.Riders.Union(Enumerable.Repeat(ride.Driver, 1));
				string key = Subset.GetKey(participants);
				Subset subset;
				if (!subsets.TryGetValue(key, out subset))
				{
					subset = new Subset(participants);
					subsets.Add(key, subset);
				}
				subset.AddRide(ride.Driver);
			}

			Console.Write("DRIVER->".PadLeft(10));
			foreach (var person in people)
				Console.Write(person.ToString().PadLeft(10));
			Console.WriteLine();
			foreach (var subset in subsets.Values.OrderByDescending(s => s.Participants.Count))
			{
				string codes = String.Join("", subset.Participants.OrderBy(p => p.Id).Select(p => p.Code));
				Console.Write(codes.PadLeft(10));
				foreach (var person in people)
				{
					int drives; 
					if (subset.Drives.TryGetValue(person, out drives)) 
						Console.Write(drives.ToString().PadLeft(10));
					else 
						Console.Write("".PadLeft(10));
				}
				Console.WriteLine();
			}

			if (verbose)
			{
				int i = 0;
				var occurencesPerPoolSize = subsets.Values
                    .GroupBy(s => s.Participants.Count)
                    .Select(g => new { 
                            PoolSize = g.Key, 
                            PerPerson = people.Select(p => new { 
                                Person = p,
                                Drives = g.Sum(s => s.Drives.Where(d => d.Key == p).Sum(d => d.Value)),
                                Participations = g.Where(s => s.Participants.Contains(p)).Sum(s => s.Drives.Sum(d => d.Value))
                            }
				).ToDictionary(x => x.Person, x => new { x.Drives, x.Participations })
                        }
				)
                    .OrderByDescending(x => x.PoolSize);

				var faird = new float[people.Count];
				var actuald = new int[people.Count];

				Console.WriteLine("\nParticipations in pool size:");
				foreach (var poolSet in occurencesPerPoolSize)
				{
					Console.Write(("b" + poolSet.PoolSize).PadLeft(10));
					i = 0;
					foreach (var person in people)
					{
						int participations = 0; 
						if (poolSet.PerPerson.ContainsKey(person))
							participations = poolSet.PerPerson [person].Participations;
						Console.Write(participations.ToString().PadLeft(10));
						faird [i++] += (float)participations / poolSet.PoolSize;
					}
					Console.WriteLine();
				}

				Console.WriteLine("\nDrives in pool size:");
				foreach (var poolSet in occurencesPerPoolSize)
				{
					Console.Write(("d" + poolSet.PoolSize).PadLeft(10));
					i = 0;
					foreach (var person in people)
					{
						int drives = 0; 
						if (poolSet.PerPerson.ContainsKey(person))
							drives = poolSet.PerPerson [person].Drives;
						Console.Write(drives.ToString().PadLeft(10));
						actuald [i++] += drives;
					}
					Console.WriteLine();
				}

				Console.WriteLine("\nCurrent Fairness:");
				Console.Write(("fair d").PadLeft(10));
				i = 0;
				foreach (var person in people)
				{
					Console.Write(faird [i++].ToString("0.#").PadLeft(10));
				}
				Console.WriteLine();
				Console.Write("actual d".PadLeft(10));
				i = 0;
				foreach (var person in people)
				{
					Console.Write(actuald [i++].ToString().PadLeft(10));
				}
				Console.WriteLine();
			}

			var activeSubsetKey = Subset.GetKey(peopleHere);
			Subset activeSubset; 
			if (subsets.TryGetValue(activeSubsetKey, out activeSubset))
				return GetSelectionsFromScores(activeSubset.Drives);
			else
				return null;

		}

		// Unfair
		// Rafal's spreadsheet
		public static IEnumerable<IEnumerable<Person>> PointsMethod(IList<Person> people, IList<Ride> rides)
		{
			Console.Write("PERSON->".PadLeft(10));
			foreach (var person in people)
				Console.Write(person.ToString().PadLeft(10));
			Console.WriteLine();

			var totalScores = new int[people.Count];
			foreach (var ride in rides)
			{
				var scores = new int[people.Count];
				scores [ride.Driver.Id] += ride.Riders.Count();
				foreach (var rider in ride.Riders)
					scores [rider.Id] -= 1;

				Trace.Assert(scores.Sum() == 0);
				for (int i = 0; i < people.Count; i++)
					totalScores [i] += scores [i];

				if (verbose)
				{
					Console.Write("".PadLeft(10));
					foreach (var person in people)
						Console.Write(scores [person.Id].ToString().PadLeft(10));
					Console.WriteLine();
				}
			}

			if (verbose)
				Console.WriteLine("Final Scores:");
			Console.Write("".PadLeft(10));
			foreach (var person in people)
				Console.Write(totalScores [person.Id].ToString().PadLeft(10));
			Console.WriteLine();

			return GetSelectionsFromScores(
                Enumerable.Range(0, totalScores.Length).ToDictionary(i => people.Single(x => x.Id == i), i => totalScores [i]));
		}

		// Unfair
		// My first version
		public static IEnumerable<IEnumerable<Person>> PairsMethod(IList<Person> people, IList<Ride> rides, IList<Person> peopleHere)
		{
			var noOfPeople = people.Count;
			var matrix = new uint[noOfPeople, noOfPeople];
			foreach (var ride in rides)
				foreach (var rider in ride.Riders) 
					matrix [ride.Driver.Id, rider.Id]++;

			var disparities = new List<Disparity>();
			for (uint i = 0; i < noOfPeople; i++)
			{
				for (uint j = 0; j < noOfPeople; j++)
				{
					if (matrix [i, j] < matrix [j, i])
					{
						uint magnitude = matrix [j, i] - matrix [i, j];
						disparities.Add(new Disparity(people [(int)j], people [(int)i], magnitude));
					}
				}
			}

			// Print the matrix
			if (verbose)
			{
				Console.Write("DRIVER->".PadLeft(10));
				foreach (var person in people)
				{
					Console.Write(person.ToString().PadLeft(10));
				}
				Console.WriteLine();

				for (uint i = 0; i < noOfPeople; i++)
				{
					Console.Write(people [(int)i].ToString().PadLeft(10));
					for (uint j = 0; j < noOfPeople; j++)
					{
						Console.Write((i == j ? "X" : matrix [j, i].ToString()).PadLeft(10));
					}
					Console.WriteLine();
				}

				Console.WriteLine();
			}

			// Remove missing people
			disparities = disparities.Where(d => peopleHere.Contains(d.Driver) && peopleHere.Contains(d.Rider)).ToList();

			// Print the disparities
			Console.WriteLine(disparities.Count > 0 ? "Current disparities:" : "There are no disparities!");
			foreach (var disparity in disparities)
			{
				Console.WriteLine("\t" + disparity);
			}            
           
			return disparities
                .GroupBy(d => d.Rider, (x,y) => new { Driver = x, GroupMagnitude = y.Sum(z => z.Magnitude), MaxMagnitude = y.Max(z => z.Magnitude)})
                .GroupBy(g => new { g.MaxMagnitude, g.GroupMagnitude })
                .OrderByDescending(g => g.Key.MaxMagnitude).ThenByDescending(g => g.Key.GroupMagnitude)
                .Select(g => g.Select(d => d.Driver));
		}
	}
}
