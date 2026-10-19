$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dbSql = Get-Content -Path (Join-Path $scriptDir "..\BharatTrustBank.Database\Database.sql") -Raw
$seedSql = Get-Content -Path (Join-Path $scriptDir "..\BharatTrustBank.Database\SeedData.sql") -Raw

# Convert Database.sql
$dbSql = $dbSql -replace "(?m)^IF NOT EXISTS \(SELECT \* FROM sys\.databases.*(?s).*?GO\s*", ""
$dbSql = $dbSql -replace "(?mi)^USE BharatTrustBankDB;\s*GO\s*", ""
$dbSql = $dbSql -replace "(?mi)^IF OBJECT_ID\('(.*?)', 'U'\) IS NOT NULL DROP TABLE \1;\s*", "DROP TABLE IF EXISTS `$1;`r`n"
$dbSql = $dbSql -replace "(?i)INT IDENTITY\(1,1\) PRIMARY KEY", "INTEGER PRIMARY KEY AUTOINCREMENT"
$dbSql = $dbSql -replace "(?i)INT NOT NULL FOREIGN KEY REFERENCES ([a-zA-Z0-9_]+)\(([a-zA-Z0-9_]+)\)( ON DELETE CASCADE)?", "INTEGER NOT NULL REFERENCES `$1(`$2)`$3"
$dbSql = $dbSql -replace "(?i)INT NULL FOREIGN KEY REFERENCES ([a-zA-Z0-9_]+)\(([a-zA-Z0-9_]+)\)( ON DELETE CASCADE)?", "INTEGER REFERENCES `$1(`$2)`$3"
$dbSql = $dbSql -replace "(?i)INT FOREIGN KEY REFERENCES ([a-zA-Z0-9_]+)\(([a-zA-Z0-9_]+)\)( ON DELETE CASCADE)?", "INTEGER REFERENCES `$1(`$2)`$3"
$dbSql = $dbSql -replace "(?i)DATETIME DEFAULT GETDATE\(\)", "DATETIME DEFAULT CURRENT_TIMESTAMP"
$dbSql = $dbSql -replace "(?i)DATE DEFAULT GETDATE\(\)", "DATE DEFAULT CURRENT_DATE"
$dbSql = $dbSql -replace "(?i)NVARCHAR\(MAX\)", "TEXT"
$dbSql = $dbSql -replace "(?mi)^\s*GO\s*$", ""
$dbSql = $dbSql -replace "(?i)BIT DEFAULT 1", "INTEGER DEFAULT 1"
$dbSql = $dbSql -replace "(?i)BIT DEFAULT 0", "INTEGER DEFAULT 0"
$dbSql = $dbSql -replace "(?i)BIT NOT NULL", "INTEGER NOT NULL"
$dbSql = $dbSql -replace "(?i)DATEADD\(year,\s*1,\s*GETDATE\(\)\)", "datetime('now', '1 years')"

# Convert SeedData.sql
$seedSql = $seedSql -replace "(?mi)^USE BharatTrustBankDB;\s*GO\s*", ""
$seedSql = $seedSql -replace "(?mi)^\s*GO\s*$", ""
$seedSql = $seedSql -replace "(?i)DATEADD\(day,\s*(-?\d+),\s*GETDATE\(\)\)", "datetime('now', '`$1 days')"
$seedSql = $seedSql -replace "(?i)DATEADD\(month,\s*(-?\d+),\s*GETDATE\(\)\)", "datetime('now', '`$1 months')"
$seedSql = $seedSql -replace "(?i)DATEADD\(year,\s*(-?\d+),\s*GETDATE\(\)\)", "datetime('now', '`$1 years')"
$seedSql = $seedSql -replace "(?i)GETDATE\(\)", "CURRENT_TIMESTAMP"

# Fix RIGHT function for SQLite
$seedSql = $seedSql -replace "(?i)RIGHT\('000' \+ CAST\(UserId AS VARCHAR\(5\)\), 4\)", "substr('000' || CAST(UserId AS TEXT), -4)"
$seedSql = $seedSql -replace "(?i)RIGHT\('00000' \+ CAST\(UserId AS VARCHAR\(5\)\), 6\)", "substr('00000' || CAST(UserId AS TEXT), -6)"
$seedSql = $seedSql -replace "(?i)RIGHT\(CAST\(UserId \* 1111 AS VARCHAR\(10\)\), 4\)", "substr(CAST(UserId * 1111 AS TEXT), -4)"
$seedSql = $seedSql -replace "(?i)RIGHT\('0000' \+ CAST\(UserId \* 777 AS VARCHAR\(10\)\), 4\)", "substr('0000' || CAST(UserId * 777 AS TEXT), -4)"
$seedSql = $seedSql -replace "(?i)RIGHT\('000000' \+ CAST\(c.CustomerId AS VARCHAR\(10\)\), 6\)", "substr('000000' || CAST(c.CustomerId AS TEXT), -6)"

# Fix string concatenation in SQLite
$seedSql = $seedSql -replace "(?i)'EMP' \+", "'EMP' ||"
$seedSql = $seedSql -replace "(?i)'CUST' \+", "'CUST' ||"
$seedSql = $seedSql -replace "(?i)'ABCPS' \+", "'ABCPS' ||"
$seedSql = $seedSql -replace "(?i)\+ 'K'", "|| 'K'"
$seedSql = $seedSql -replace "(?i)'2345 6789 ' \+", "'2345 6789 ' ||"
$seedSql = $seedSql -replace "(?i)'100100' \+", "'100100' ||"
$seedSql = $seedSql -replace "(?i)'200200' \+", "'200200' ||"

# Write back to DbSetup
Set-Content -Path (Join-Path $scriptDir "Database_SQLite.sql") -Value $dbSql
Set-Content -Path (Join-Path $scriptDir "SeedData_SQLite.sql") -Value $seedSql
