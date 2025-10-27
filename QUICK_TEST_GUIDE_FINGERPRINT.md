# Quick Test Guide - Fingerprint Login & Authentication

## Prerequisites
1. ? Fingerprint scanner connected via USB
2. ? Database running (PostgreSQL)
3. ? Application running (`dotnet run` or F5 in Visual Studio)
4. ? At least one user account created

---

## Test 1: First-Time Fingerprint Registration

### Steps:
1. **Login** with email and password (normal login)
2. **Navigate** to:
 - **Admin:** `/Admin/AdminRegisterFingerprint`
   - **Faculty:** `/Faculty/FacultyRegisterFingerprint`
   - **Student:** `/Student/StudentRegisterFingerprint`

3. **Check Scanner Status:**
   - Should show "Online" (green)
   - If "Offline" (red), click "Initialize Device" button

4. **Check Registration Status:**
   - Should show "Not Registered" (orange/pending)

5. **Scan Fingerprint:**
   - Click "Scan Fingerprint" button
   - Place your finger on the scanner
   - Hold steady until scanner circle turns green
   - You should see your fingerprint image appear in the circle (white background, black lines)

6. **Register Fingerprint:**
   - Click "Register Fingerprint" button
   - Wait for success message
   - Should redirect to dashboard

7. **Verify in Database:**
   ```sql
   SELECT * FROM fingerprint_templates WHERE user_id = <YOUR_USER_ID>;
   ```
   - Should show 1 record with your fingerprint data

### Expected Results:
- ? Scanner status: Online
- ? Fingerprint image displayed in circle
- ? Green LED + beep sound on scan
- ? Success message: "Fingerprint registered successfully!"
- ? Registration status changes to "Registered"
- ? Redirects to dashboard after 2 seconds

---

## Test 2: Fingerprint Login (First Time)

### Steps:
1. **Logout** from application
2. **Go to Login Page:** `/Auth/Index`
3. **Click** "Biometric Scan" button
4. **Place finger** on scanner
5. **Wait** for authentication

### Expected Results:
- ? Scanner circle shows "scanning" state (blue border)
- ? Green LED + beep on successful match
- ? Success message: **"Scan successful! Redirecting to your account..."**
- ? Redirects to appropriate dashboard:
   - Admin ? `/Admin`
   - Faculty ? `/Faculty`
   - Student ? `/Student`
- ? Session created with user details
- ? Last login timestamp updated in database

### Failure Test:
1. **Use unregistered finger** (different finger or another person)
2. **Expected:** Red LED + beep
3. **Message:** "Fingerprint not recognized. This fingerprint is not registered in the system."

---

## Test 3: Update Existing Fingerprint (Password Required)

### Steps:
1. **Login** (email/password or fingerprint)
2. **Navigate** to fingerprint registration page
3. **Registration Status** should show "Registered"
4. **Scan NEW Fingerprint:**
   - Click "Scan Fingerprint"
   - Place finger (can use same or different finger)
   - Wait for green confirmation

5. **Attempt to Register:**
   - Click "Register Fingerprint"
   - **Password modal should appear**
   - Message: "You already have a registered fingerprint. To update it, please confirm your password first."

6. **Enter Wrong Password:**
   - Type incorrect password
   - Click "Confirm & Update"
 - **Expected:** Error message: "Invalid password. Please try again."

7. **Enter Correct Password:**
   - Type correct password
   - Click "Confirm & Update"
   - **Expected:** Success message: "Fingerprint updated successfully!"

8. **Verify in Database:**
   ```sql
   SELECT ftemp_id, user_id, ftemp_name, ftemp_created_at 
   FROM fingerprint_templates 
   WHERE user_id = <YOUR_USER_ID>;
   ```
   - Should still show only 1 record (updated, not new)

### Expected Results:
- ? Password confirmation modal appears
- ? Invalid password rejected with error message
- ? Valid password accepted
- ? Fingerprint template updated in database (same `ftemp_id`)
- ? Success message shown
- ? Can login with NEW fingerprint after update

---

## Test 4: Multiple Users Fingerprint Login

### Setup:
1. Create 3 test users (Admin, Faculty, Student)
2. Register fingerprint for each user
3. Logout

### Steps:
1. **Login with User 1 fingerprint:**
   - Place User 1's finger on scanner
   - Should login as User 1
   - Verify session has User 1 details

2. **Logout and Login with User 2 fingerprint:**
   - Place User 2's finger on scanner
   - Should login as User 2
   - Verify session has User 2 details

3. **Try User 3's finger for User 1's account:**
   - Should fail with "Fingerprint not recognized"

### Expected Results:
- ? Each user can login with their own fingerprint
- ? Cross-fingerprint authentication fails
- ? Correct user session created for each login

---

## Test 5: Edge Cases

### Case 1: Scanner Disconnected
1. Unplug scanner during scan
2. **Expected:** Error message: "Failed to capture fingerprint"
3. Plug scanner back in
4. Click "Initialize Device"
5. Try scan again - should work

### Case 2: Database Offline
1. Stop PostgreSQL service
2. Try to login with fingerprint
3. **Expected:** Error message: "Authentication error"
4. Start PostgreSQL service
5. Try again - should work

### Case 3: Inactive Account
1. Set `is_active = false` in users table for test user
   ```sql
   UPDATE users SET is_active = false WHERE user_id = <TEST_USER_ID>;
   ```
2. Try to login with fingerprint
3. **Expected:** "Your account has been deactivated. Please contact administration."

### Case 4: Poor Quality Scan
1. Place finger partially on scanner
2. **Expected:** Red LED + beep
3. Message: "Failed to capture fingerprint. Please try again."

---

## Verification Checklist

### After Each Test:
- [ ] Check browser console for errors
- [ ] Verify LED lights (green = success, red = error)
- [ ] Verify beep sounds
- [ ] Check database updates:
  ```sql
  -- Check fingerprint template
  SELECT * FROM fingerprint_templates WHERE user_id = <USER_ID>;
  
  -- Check last login timestamp
  SELECT user_email, user_last_login FROM users WHERE user_id = <USER_ID>;
  
  -- Check all registered fingerprints
  SELECT u.user_email, ft.ftemp_name, ft.ftemp_created_at 
  FROM fingerprint_templates ft
  JOIN users u ON ft.user_id = u.user_id;
  ```
- [ ] Check application logs for errors
- [ ] Verify session creation
- [ ] Verify redirects

---

## Debugging Tips

### Issue: Scanner always shows "Offline"
**Solution:**
1. Check USB connection
2. Open Device Manager (Windows)
3. Look for "ZKTeco Fingerprint Reader"
4. Update/reinstall driver if needed
5. Restart application

### Issue: Fingerprint not recognized (but should be)
**Solution:**
1. Lower match threshold in `FingerprintAuthController.cs`:
   ```csharp
   int matchThreshold = 30; // Try lower value
   ```
2. Re-scan fingerprint (make sure finger is clean and dry)
3. Check database for template data:
   ```sql
   SELECT ftemp_id, user_id, LENGTH(ftemp_data) as template_size 
   FROM fingerprint_templates;
   ```

### Issue: Password modal not appearing
**Solution:**
1. Check browser console for JavaScript errors
2. Verify `requirePasswordConfirmation` is returned from backend
3. Check `register_fingerprint.js` for modal creation code

### Issue: Database errors
**Solution:**
1. Verify PostgreSQL is running
2. Check connection string in `appsettings.json`
3. Run database migrations if needed
4. Check table exists:
   ```sql
   SELECT table_name FROM information_schema.tables 
   WHERE table_name = 'fingerprint_templates';
   ```

---

## Performance Benchmarks

### Expected Response Times:
- **Scanner initialization:** < 1 second
- **Fingerprint capture:** < 2 seconds
- **Template matching:** < 500ms (for ~100 templates)
- **Database lookup:** < 100ms
- **Total login time:** < 3 seconds

### Match Accuracy:
- **True Positive Rate:** > 98% (correctly identifies registered user)
- **False Positive Rate:** < 0.1% (incorrectly matches wrong user)
- **False Rejection Rate:** < 2% (fails to recognize registered user)

---

## Quick Reference

### Important URLs:
```
Login Page: /Auth/Index
Fingerprint Login: /Auth/FingerprintLogin

Admin Registration: /Admin/AdminRegisterFingerprint
Faculty Registration: /Faculty/FacultyRegisterFingerprint
Student Registration: /Student/StudentRegisterFingerprint
```

### Database Queries:
```sql
-- View all registered fingerprints
SELECT u.user_email, ft.ftemp_name, ft.ftemp_created_at 
FROM fingerprint_templates ft
JOIN users u ON ft.user_id = u.user_id;

-- Delete fingerprint for specific user
DELETE FROM fingerprint_templates WHERE user_id = <USER_ID>;

-- Reset all fingerprints (testing only!)
TRUNCATE TABLE fingerprint_templates;
```

### Session Keys:
```
UserId - int
RoleId - int
UserEmail - string
UserName - string
UserProfileImage - string
```

---

## Test Report Template

```
Test Date: _______________
Tester: _______________

Test 1: First-Time Registration
- Scanner Status: ? Pass ? Fail
- Fingerprint Scan: ? Pass ? Fail
- Image Display: ? Pass ? Fail
- Registration: ? Pass ? Fail
Notes: _____________________

Test 2: Fingerprint Login
- Authentication: ? Pass ? Fail
- Message Display: ? Pass ? Fail
- Session Creation: ? Pass ? Fail
- Redirect: ? Pass ? Fail
Notes: _____________________

Test 3: Update with Password
- Password Modal: ? Pass ? Fail
- Invalid Password: ? Pass ? Fail
- Valid Password: ? Pass ? Fail
- Database Update: ? Pass ? Fail
Notes: _____________________

Overall Result: ? All Tests Passed ? Some Tests Failed

Signature: _______________
```

---

**Ready to test!** Follow the steps above and check off each item. ??
