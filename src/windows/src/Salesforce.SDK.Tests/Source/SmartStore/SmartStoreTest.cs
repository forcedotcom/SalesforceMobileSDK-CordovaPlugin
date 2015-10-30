/*
 * Copyright (c) 2014, salesforce.com, inc.
 * All rights reserved.
 * Redistribution and use of this software in source and binary forms, with or
 * without modification, are permitted provided that the following conditions
 * are met:
 * - Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer.
 * - Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * - Neither the name of salesforce.com, inc. nor the names of its contributors
 * may be used to endorse or promote products derived from this software without
 * specific prior written permission of salesforce.com, inc.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json.Linq;
using SQLitePCL;
using Salesforce.SDK.App;
using Salesforce.SDK.Core;
using Salesforce.SDK.Logging;

namespace Salesforce.SDK.SmartStore.Store
{
    [TestClass]
    public class SmartStoreTest
    {
        private const String BUDGET = "budget";
        private const String NAME = "name";
        private const String SALARY = "salary";
        private const String MANAGER_ID = "managerId";
        private const String EMPLOYEE_ID = "employeeId";
        private const String LAST_NAME = "lastName";
        private const String FIRST_NAME = "firstName";
        private const String DEPT_CODE = "deptCode";
        private const String EMPLOYEES_SOUP = "employees";
        private const String DEPARTMENTS_SOUP = "departments";
        internal const string SoupEntryId = "_soupEntryId";
        internal const string SoupLastModifiedDate = "_soupLastModifiedDate";
        private static SmartStore Store;

        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            SFApplicationHelper.RegisterServices();
            SDKServiceLocator.RegisterService<ILoggingService, Hybrid.Logging.Logger>();
            Store = SmartStore.GetGlobalSmartStore();
            SetupData();
        }

        private static void SetupData()
        {
            Store.ResetDatabase();

            Store.RegisterSoup(EMPLOYEES_SOUP, new[]
            {
                // should be TABLE_1
                new IndexSpec(FIRST_NAME, SmartStoreType.SmartString), // should be TABLE_1_0
                new IndexSpec(LAST_NAME, SmartStoreType.SmartString), // should be TABLE_1_1
                new IndexSpec(DEPT_CODE, SmartStoreType.SmartString), // should be TABLE_1_2
                new IndexSpec(EMPLOYEE_ID, SmartStoreType.SmartString), // should be TABLE_1_3
                new IndexSpec(MANAGER_ID, SmartStoreType.SmartString), // should be TABLE_1_4
                new IndexSpec(SALARY, SmartStoreType.SmartInteger)
            }); // should be TABLE_1_5
            Store.RegisterSoup(DEPARTMENTS_SOUP, new[]
            {
                // should be TABLE_2
                new IndexSpec(DEPT_CODE, SmartStoreType.SmartString), // should be TABLE_2_0
                new IndexSpec(NAME, SmartStoreType.SmartString), // should be TABLE_2_1
                new IndexSpec(BUDGET, SmartStoreType.SmartInteger)
            }); // should be TABLE_2_2
            LoadData();
        }

        /// <summary>
        ///     Testing simple smart sql to sql conversion2
        [TestMethod]
        public void TestSimpleConvertSmartSql()
        {
            Assert.AreEqual("select TABLE_1_0, TABLE_1_1 from TABLE_1 order by TABLE_1_1",
                Store.ConvertSmartSql(
                    "select {employees:firstName}, {employees:lastName} from {employees} order by {employees:lastName}"));
            Assert.AreEqual("select TABLE_2_1 from TABLE_2 order by TABLE_2_0",
                Store.ConvertSmartSql("select {departments:name} from {departments} order by {departments:deptCode}"));
        }

        /// <summary>
        ///     Testing simple smart sql to sql conversion2
        [TestMethod]
        public void TestDeleteDb()
        {
            Assert.AreEqual("select TABLE_1_0, TABLE_1_1 from TABLE_1 order by TABLE_1_1",
                Store.ConvertSmartSql(
                    "select {employees:firstName}, {employees:lastName} from {employees} order by {employees:lastName}"));
            SmartStore.DeleteAllDatabases(true);
            try
            {
                Store.HasSoup(DEPARTMENTS_SOUP);
                Assert.Fail("Table exists");
            }
            catch (SQLiteException)
            {
                // we're good, table doesn't exist
            }
            finally
            {
                SetupData();
            }
        }

        /// <summary>
        ///     Testing smart sql to sql conversion when there is a join
        /// </summary>
        [TestMethod]
        public void TestConvertSmartSqlWithJoin()
        {
            Assert.AreEqual("select TABLE_2_1, TABLE_1_0 || ' ' || TABLE_1_1 "
                            + "from TABLE_1, TABLE_2 "
                            + "where TABLE_2_0 = TABLE_1_2 "
                            + "order by TABLE_2_1, TABLE_1_1",
                Store.ConvertSmartSql("select {departments:name}, {employees:firstName} || ' ' || {employees:lastName} "
                                      + "from {employees}, {departments} "
                                      + "where {departments:deptCode} = {employees:deptCode} "
                                      + "order by {departments:name}, {employees:lastName}"));
        }

        /// <summary>
        ///     Testing smart sql to sql conversion when there is a self join
        /// </summary>
        [TestMethod]
        public void TestConvertSmartSqlWithSelfJoin()
        {
            Assert.AreEqual("select mgr.TABLE_1_1, e.TABLE_1_1 "
                            + "from TABLE_1 as mgr, TABLE_1 as e "
                            + "where mgr.TABLE_1_3 = e.TABLE_1_4",
                Store.ConvertSmartSql("select mgr.{employees:lastName}, e.{employees:lastName} "
                                      + "from {employees} as mgr, {employees} as e "
                                      + "where mgr.{employees:employeeId} = e.{employees:managerId}"));
        }

        /// <summary>
        ///     Testing smart sql to sql conversion when path is: _soup, _soupEntryId or _soupLastModifiedDate
        /// </summary>
        [TestMethod]
        public void TestConvertSmartSqlWithSpecialColumns()
        {
            Assert.AreEqual("select TABLE_1.id, TABLE_1.lastModified, TABLE_1.soup from TABLE_1",
                Store.ConvertSmartSql(
                    "select {employees:_soupEntryId}, {employees:_soupLastModifiedDate}, {employees:_soup} from {employees}"));
        }

        /// <summary>
        ///     Testing smart sql to sql conversion when path is: _soup, _soupEntryId or _soupLastModifiedDate and there is a join
        /// </summary>
        [TestMethod]
        public void TestConvertSmartSqlWithSpecialColumnsAndJoin()
        {
            Assert.AreEqual("select TABLE_1.id, TABLE_2.id from TABLE_1, TABLE_2",
                Store.ConvertSmartSql(
                    "select {employees:_soupEntryId}, {departments:_soupEntryId} from {employees}, {departments}"));
        }

        /// <summary>
        ///     Testing smart sql to sql conversion when path is: _soup, _soupEntryId or _soupLastModifiedDate and there is a self
        ///     join
        /// </summary>
        [TestMethod]
        public void TestConvertSmartSqlWithSpecialColumnsAndSelfJoin()
        {
            Assert.AreEqual("select mgr.id, e.id from TABLE_1 as mgr, TABLE_1 as e",
                Store.ConvertSmartSql(
                    "select mgr.{employees:_soupEntryId}, e.{employees:_soupEntryId} from {employees} as mgr, {employees} as e"));
        }

        /// <summary>
        ///     Test smart sql to sql conversation with insert/update/delete: expect exception
        /// </summary>
        [TestMethod]
        public void TestConvertSmartSqlWithInsertUpdateDelete()
        {
            foreach (
                string smartSql in new[] {"insert into {employees}", "update {employees}", "delete from {employees}"})
            {
                try
                {
                    Store.ConvertSmartSql(smartSql);
                    Assert.Fail("Should have thrown exception for " + smartSql);
                }
                catch (SmartStoreException e)
                {
                    // Expected
                }
            }
        }

        [TestMethod]
        public void TestSmartQueryDoingCount()
        {
            JArray result = Store.Query(QuerySpec.BuildSmartQuerySpec("select count(*) from {employees}", 1), 0);
            JArray compare = JArray.Parse("[[7]]");
            Assert.AreEqual(compare.ToString(), result.ToString());
        }

        /// <summary>
        ///     Test running smart query that does a select sum
        /// </summary>
        [TestMethod]
        public void TestSmartQueryDoingSum()
        {
            JArray result =
                Store.Query(QuerySpec.BuildSmartQuerySpec("select sum({departments:budget}) from {departments}", 1), 0);
            JArray compare = JArray.Parse("[[3000000]]");
            Assert.AreEqual(compare.ToString(), result.ToString());
        }

        /// <summary>
        ///     Test running smart query that return one row with one integer
        /// </summary>
        [TestMethod]
        public void TestSmartQueryReturningOneRowWithOneInteger()
        {
            JArray result =
                Store.Query(
                    QuerySpec.BuildSmartQuerySpec(
                        "select {employees:salary} from {employees} where {employees:lastName} = 'Haas'", 1), 0);
            JArray compare = JArray.Parse("[[200000]]");
            Assert.AreEqual(compare.ToString(), result.ToString());
        }

        /// <summary>
        ///     Test running smart query that return one row with two integers
        /// </summary>
        [TestMethod]
        public void TestSmartQueryReturningOneRowWithTwoIntegers()
        {
            JArray result =
                Store.Query(
                    QuerySpec.BuildSmartQuerySpec(
                        "select mgr.{employees:salary}, e.{employees:salary} from {employees} as mgr, {employees} as e where e.{employees:lastName} = 'Thompson' and mgr.{employees:employeeId} = e.{employees:managerId}",
                        1), 0);
            JArray compare = JArray.Parse("[[200000,120000]]");
            Assert.AreEqual(compare.ToString(), result.ToString());
        }

        /// <summary>
        ///     Test running smart query that return two rows with one integer each
        /// </summary>
        [TestMethod]
        public void TestSmartQueryReturningTwoRowsWithOneIntegerEach()
        {
            JArray result =
                Store.Query(
                    QuerySpec.BuildSmartQuerySpec(
                        "select {employees:salary} from {employees} where {employees:managerId} = '00010' order by {employees:firstName}",
                        2), 0);
            JArray compare = JArray.Parse("[[120000],[100000]]");
            Assert.AreEqual(compare.ToString(), result.ToString());
        }

        /// <summary>
        ///     Test running smart query that return a soup along with a string and an integer
        /// </summary>
        [TestMethod]
        public void TestSmartQueryReturningSoupStringAndInteger()
        {
            JArray query = Store.Query(QuerySpec.BuildExactQuerySpec(EMPLOYEES_SOUP, "employeeId", "00010", 1), 0);
            var christineJson = query[0] as JObject;
            Assert.AreEqual("Christine", christineJson.GetValue(FIRST_NAME).ToString(), "Wrong elt");

            JArray result =
                Store.Query(
                    QuerySpec.BuildSmartQuerySpec(
                        "select {employees:_soup}, {employees:firstName}, {employees:salary} from {employees} where {employees:lastName} = 'Haas'",
                        1), 0);
            Assert.AreEqual(1, result.Count(), "Expected one row");
            Assert.AreEqual(christineJson.ToString(), result[0][0].ToString(), "Wrong soup");
            Assert.AreEqual("Christine", result[0][1].ToString(), "Wrong first name");
            Assert.AreEqual(200000, (int) (result[0][2]), "Wrong salary");
        }

        /// <summary>
        ///     Test running smart query with paging
        /// </summary>
        [TestMethod]
        public void TestSmartQueryWithPaging()
        {
            QuerySpec query =
                QuerySpec.BuildSmartQuerySpec(
                    "select {employees:firstName} from {employees} order by {employees:firstName}", 1);
            Assert.AreEqual(7, Store.CountQuery(query), "Expected 7 employees");

            String[] expectedResults = {"Christine", "Eileen", "Eva", "Irving", "John", "Michael", "Sally"};
            for (int i = 0; i < 7; i++)
            {
                JArray result = Store.Query(query, i);
                Assert.AreEqual(JArray.Parse("[[\"" + expectedResults[i] + "\"]]").ToString(), result.ToString(),
                    "Wrong result at page " + i);
            }
        }

        /// <summary>
        ///     Test running smart query that targets _soup, _soupEntryId and _soupLastModifiedDate
        /// </summary>
        [TestMethod]
        public void TestSmartQueryWithSpecialFields()
        {
            var christineJson =
                Store.Query(QuerySpec.BuildExactQuerySpec(EMPLOYEES_SOUP, "employeeId", "00010", 1), 0)[0] as JObject;
            Assert.AreEqual("Christine", christineJson.GetValue(FIRST_NAME), "Wrong elt");

            JArray result =
                Store.Query(
                    QuerySpec.BuildSmartQuerySpec(
                        "select {employees:_soup}, {employees:_soupEntryId}, {employees:_soupLastModifiedDate}, {employees:salary} from {employees} where {employees:lastName} = 'Haas'",
                        1), 0);
            Assert.AreEqual(1, result.Count, "Expected one row");
            Assert.AreEqual(christineJson.ToString(), result[0][0].ToString(), "Wrong soup");
            Assert.AreEqual(christineJson.GetValue(SoupEntryId), result[0][1], "Wrong soupEntryId");
            Assert.AreEqual(christineJson.GetValue(SoupLastModifiedDate), result[0][2], "Wrong soupLastModifiedDate");
        }

        /// <summary>
        ///     Load some datq in the smart store
        /// </summary>
        private static void LoadData()
        {
            // Employees
            CreateEmployee("Christine", "Haas", "A00", "00010", null, 200000);
            CreateEmployee("Michael", "Thompson", "A00", "00020", "00010", 120000);
            CreateEmployee("Sally", "Kwan", "A00", "00310", "00010", 100000);
            CreateEmployee("John", "Geyer", "B00", "00040", null, 102000);
            CreateEmployee("Irving", "Stern", "B00", "00050", "00040", 100000);
            CreateEmployee("Eva", "Pulaski", "B00", "00060", "00050", 80000);
            CreateEmployee("Eileen", "Henderson", "B00", "00070", "00050", 70000);

            // Departments
            CreateDepartment("A00", "Sales", 1000000);
            CreateDepartment("B00", "R&D", 2000000);
        }

        private static void CreateEmployee(string firstName, string lastName, string deptCode, string employeeId,
            string managerId, int salary)
        {
            var employee = new JObject();
            employee.Add(FIRST_NAME, firstName);
            employee.Add(LAST_NAME, lastName);
            employee.Add(DEPT_CODE, deptCode);
            employee.Add(EMPLOYEE_ID, employeeId);
            employee.Add(MANAGER_ID, managerId);
            employee.Add(SALARY, salary);
            Store.Create(EMPLOYEES_SOUP, employee);
        }

        private static void CreateDepartment(String deptCode, String name, int budget)
        {
            var department = new JObject();
            department.Add(DEPT_CODE, deptCode);
            department.Add(NAME, name);
            department.Add(BUDGET, budget);
            Store.Create(DEPARTMENTS_SOUP, department);
        }
    }
}