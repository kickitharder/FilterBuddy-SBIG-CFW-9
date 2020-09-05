#define NAME "FilterBuddy"
#define VERSION "CFW-9 V1.200902"
#define BY "Keith Rickard"

#include <Wire.h>
#define BUTTON_PIN 4
//IMPORTANT - indicator LEDs are connected to PWM pins
#define LED1 11
#define LED2 10
#define LED3 9
#define LED4 6
#define LED5 5

byte error, fwError, unplug, currentSlot, targetSlot, ascom, testMode, statusByte;
char cmd;
unsigned long timer;
//==============================================================================================
void setup() {
  testMode = 0;
  error = fwError = unplug = currentSlot = ascom = statusByte = cmd = 0;
  targetSlot = 1;

  Wire.begin();                                                 // Start I2C interface
  Serial.begin(9600);                                           // Start Serial interface
  pinMode(BUTTON_PIN, INPUT_PULLUP);                            // When button is pressed, wheel turns to next slot
  switchLEDs(B0000);                                            // Turn off all 5 indicator LEDs

  testModeSelect();
  fwInit();
  timer = millis();                                             // Timer for checking whether the CFW-9 is sill responding
}
//==============================================================================================
void loop() {
  cmd = 0;
  if (Serial.available()) {                                   // Get a Serial command
    cmd = Serial.read();                                      // and process it

    if (testMode) {
      switch (cmd) {
        case ')': errorHandler(0); break;                     // Filter wheel is OK
        case '!': errorHandler(1); break;                     // Wheel seems to be stuck
        case '"': errorHandler(2); break;                     // Wheel move timed out
        case '@': errorHandler(2); break;                     // Wheel move timed out
        case char(0xA3): errorHandler(3); break;              // I2C: CFW-9 not responding - check connection
        case '#': errorHandler(3); break;                     // I2C: CFW-9 not responding - check connection
        case '$': errorHandler(4); break;                     // I2C: Data too long to fit transmit buffer
        case '%': errorHandler(8); break;                     // I2C: No ACK received on transmission
        case '^': errorHandler(12); break;                    // I2C: Received NAK on transmit of address
        case '&': errorHandler(16); break;                    // I2C: Received NAK on transmit of data
        case '*': errorHandler(20); break;                    // I2C: Undefined error
        case '(': errorHandler(99); break;                    // Unknown error"
      }
    }

    switch (cmd) {
      case '1': gotoSlot(1); break;                           // Goto slot 1
      case '2': gotoSlot(2); break;                           // Goto slot 2
      case '3': gotoSlot(3); break;                           // Goto slot 3
      case '4': gotoSlot(4); break;                           // Goto slot 4
      case '5': gotoSlot(5); break;                           // Goto slot 5
      case '0': gotoSlot(0); break;                           // Force wheel to move to home slot (1) even if at slot 1
      case 'A': ascom = 1; Serial.print(cmd); break;          // ASCOM mode - button ignored
      case 'a': ascom = 0; Serial.print(cmd); break;          // ASCOM mode off
      case 'C': Serial.print(fwError ? 'E' : 'C'); break;     // Return 'C' indicates connected and OK, 'E' if CFW-9 error
      case 'E': errorMsg(); break;                            // Return current error message
      case 'e': resetError(); break;                          // Reset fwError condition
      case 'S': getStatusByte(); break;                       // Returns Status byte in hex
      case 'M': Serial.print(char(isMoving() + '0')); break;  // Returns 1 = moving, 0 = not moving
      case 'G': Serial.print(char(currentSlot + '0')); break; // Return current slot
      case '?': Serial.print(char(currentSlot + '0')); break; // Return current slot
      case 'R': restart(); break;                             // Restart sketch
      case 'V': Serial.print(F(VERSION)); break;              // Return version
      case 'N': Serial.print(F(NAME)); break;                 // Return name
      case 'B': Serial.print(F(BY)); break;                   // Return name of author
      case 'F': Serial.print('5'); break;                     // Return number of slots
      case 'T': testModeSelect(); break;                      // Test mode on - simulate!
      case 't': testModeSelect(); break;                      // Test mode off
      case 'U': unplug = 1; Serial.print(cmd); break;         // Simulate unplugging of CFW-9
      case 'u': unplug = 30; Serial.print(cmd); break;        // Simulate replugging of CFW-9
      default: cmd = 0; break;
    }
  }
  if (cmd) Serial.print(testMode ? "#\n" : "#");              // Send '#' to Serial to mark command actioned

  if (!ascom) {                                               // If not in ASCOM mode then respond to button presses
    byte selectedSlot = currentSlot;
    if (!fwError) {
      int buttonDelay = 0;                                    // Time to wait for next button press
      while (checkButton(1, buttonDelay)) {                   // Loop if button has been pressed
        if (checkButton(0, 1000)) {                           // Wait upto 1 sec for button to be realesed, if within 1 sec select next slot
          if (++selectedSlot > 5) selectedSlot = 1;           // Wrap round to 1 if greater than 5
          switchLEDs(selectLED(selectedSlot));                // Light up the corresponding LED
          buttonDelay = 1000;                                 // Wait upto a second for button to be pressed again
        }
      }
      if (selectedSlot != currentSlot) gotoSlot(selectedSlot); // Ready to move to new filter slot
    }
  }

  checkConnection();                                          // See if CFW-9 is still connected to FilterBuddy
}
//==============================================================================================
// COMMANDS
//==============================================================================================
void gotoSlot(byte toSlot) {
  targetSlot = !toSlot ? 1 : toSlot;
  if (errorHandler(fwError)) return;                              // Return if error not yet cleared
  if (waitForSlot()) return;                                      // Return if wheel not at a slot after waiting

  if (toSlot != (currentSlot = getSlot())) {                      // Only process if target is not the same as the current slot
    if (errorHandler(moveToSlot(toSlot))) return;                 // Start the wheel moving, return if there is an error

    byte fromLED = selectLED(currentSlot);
    byte toLED   = selectLED(targetSlot) | fromLED;
    for (byte i = 1; i <= 16; i++) {                              // Wait about 8 seconds for wheel to stop moving
      if (!(error = isMoving())) break;                           // Exit if wheel has stopped
      switchLEDs(fromLED);                                        // Do LED pattern for moving
      delay(300);
      if (!(error = isMoving())) break;                           // Exit if wheel has stopped
      switchLEDs(toLED);
      delay(150);
    }

    if (error) if (!fwError) fwError = (error & B01000000) ? 1 : 2; // 1 = Wheel is stuck, 2 = Wheel has not arrived
    if (errorHandler(fwError)) return;                            // Process any error    Serial.println("WP6");

    currentSlot = targetSlot;                                     // Yippee! Wheel has arrived at target slot.
  }

  switchLEDs(selectLED(currentSlot));
  if (cmd) Serial.print(char(currentSlot + '0'));                 // Return slot number if it was a PC command.
}
//==============================================================================================
void testModeSelect() {
  if (!cmd) {
    if (checkButton(1, 0)) testMode = 1;      // If button is pressed at startup then enter test mode
    if (testMode == 1) {                      // Announce that we are in test mode
      Serial.println();
      Serial.print(F(NAME));
      Serial.print(" ");
      Serial.println(F(VERSION));
      Serial.print(F("By "));
      Serial.println(F(BY));
      Serial.println(F("***TEST MODE***"));
      switchLEDs(B10101);                     // Test mode LED pattern
      delay(1000);
      timer = millis() + 5000;                // Timer emulates a moving wheel
    }
    testMode = (testMode >= 1);
  } else {
    testMode = (cmd == 'T') ? 1 : 0;
    Serial.print(cmd);
  }
}
//==============================================================================================
// HARDWARE STUFF
//==============================================================================================
bool checkButton(bool state, unsigned long delayTime) { // state = 0 test releasing of button, = 1 test pressing of button
  delayTime += millis();                                // Wait upto delayTime ms for button being released/pressed before quitting
  do {
    if (!digitalRead(BUTTON_PIN) == state) {            // Has button been released/pressed? (BUTTON_PIN: 1 = released, 0 = pressed)
      delay(50);                                        // Wait 50ms for debounce
      if (!digitalRead(BUTTON_PIN) == state) return 1;  // Test button again - return if it is still released/pressed
    }
  } while (millis() <= delayTime);
  return 0;                                             // Button not released/pressed within delay time, return 0.
}
//==============================================================================================
void restart() {
  Serial.println("R#");                                 // Acknowledge command
  delay(1000);                                          // Wait 1 sec
  moveToSlot(testMode = 0);                             // Force the filter wheel to turn to slot 1 even if it is as slot 1
  asm("Jmp 0");                                         // Restart this sketch
}
//==============================================================================================
void switchLEDs(byte select) {
  byte led[5] = {LED1, LED2, LED3, LED4, LED5};
  select = select & B11111;                             // Each bit of the select byte indicates whether its corresponding LED is lit
  for (byte i = 0; i <= 4; i++) {
    analogWrite(led[i], ((select & (1 << i)) > 0) * 63 + 1);  // On = 64, Off = 1
  }
}
//==============================================================================================
byte selectLED(byte led) {
  return (1 << (led - 1));                              // Returns value so that LED indicator number 'led' will be lit
}
//==============================================================================================
void fwInit() {
  if (fwError) return;                                          // Return if CFW-9 has already been initailised
  byte LEDs = 1;
  for (byte i = 1; i <= 40; i ++) {                             // Wait until timeout of 8 secs
    if (unplug > 2) unplug--;
    if (!(error = isMoving())) break;                           // Break if wheel has arrived at a slot
    if (fwError) break;                                         // Break if CFW-9 not responding
    switchLEDs(~selectLED(LEDs));                               // Peform LED initiating pattern
    delay(200);
    if (++LEDs > 5) LEDs = 1;
  }
  if (unplug == 2) unplug = 0;
  if (error) if (!fwError) fwError = (error & B01000000) ? 1 : 2; // 1 = Wheel is stuck, 2 = Wheel move timed out
    else switchLEDs(selectLED(currentSlot = slot()));             // Get the slot as reported by the CFW-9
  errorHandler(fwError);                                        // Process any error
}
//==============================================================================================
void checkConnection() {
  if (testMode) return;                                         // Return if in test mode
  if (millis() < timer) return;                                 // Only check once 1 sec has past
  timer = millis() + 1000;
  if (isMoving() == 0x10) fwInit();
  if (!errorHandler(fwError)) gotoSlot(targetSlot);
}
//==============================================================================================
// ERROR HANDLING
//==============================================================================================
byte errorHandler(byte errCode) {
  fwError = errCode;
  if (errCode >= 3) errCode = 3;                        // 3 = Filter wheel not responding
  switchLEDs(errCode ? ~selectLED(errCode + 1) : selectLED(currentSlot));
  if (fwError) {
    if (cmd) Serial.print(testMode ? "0#\n" : "0#");    // 0# = Error
    cmd = 0;                                            // Prevents 0# being sent again
    currentSlot = 0;
  }
  return fwError;
}
//==============================================================================================
void resetError() {
  if (cmd) {
    Serial.print(testMode ? "e#\n" : "?e");
    cmd = 0;
  }
  currentSlot = 1;      // Assume currentSlot for testMode
  getSlot();            // Get actual slot - could still be 0 if error still present
  fwError = 0;          // Cancel error
}
//==============================================================================================
void errorMsg() {
  switch (fwError) {
    case 0: Serial.print(F("Filter wheel is OK")); break;
    case 1: Serial.print(F("Wheel seems to be stuck")); break;
    case 2: Serial.print(F("Wheel move timed out")); break;
    case 3: Serial.print(F("I2C: CFW-9 not responding - check connection")); break;
    case 4: Serial.print(F("I2C: Data too long to fit transmit buffer")); break;
    case 8: Serial.print(F("I2C: No ACK received on transmission - check connection")); break;
    case 12: Serial.print(F("I2C: Received NAK on transmit of address - check connection")); break;
    case 16: Serial.print(F("I2C: Received NAK on transmit of data - check connection")); break;
    case 20: Serial.print(F("I2C: Undefined error")); break;
    default: Serial.print(F("Unknown error")); break;
  }
}
//==============================================================================================
// CFW-9 INTERFACE STUFF
//==============================================================================================
byte moveToSlot(byte toSlot) {
  if (testMode) {
    if (!toSlot) toSlot = 1;
    if (toSlot < currentSlot) toSlot += 5;
    timer = millis() + (800 * (toSlot - currentSlot));
    return 0;                                           // 0 = success
  }
  if (unplug == 1) return fwError = 3;
  Wire.beginTransmission(0x52);                         // Send 0xA4 0x10 0x0X (X = slot number) to CFW-9
  Wire.write(0x10);
  Wire.write(toSlot);
  return fwError = Wire.endTransmission() << 2;         // Move any error code to bits 4-6, 0 = success, (1...4)*8 = error
}
//==============================================================================================
byte getStatusByte() {
  delay(10);                                      // Let's not overwhelm the CFW-9!
  statusByte = fwError = 0;

  if (unplug > 2) statusByte = 0x10;
  if (unplug == 1) fwError = 3;
  if (!unplug) {
    fwError = (Wire.requestFrom(0x52, 1) ? 0 : 3);  // Get response from CFW-9 and receive one byte. 3 = no response from CFW-9
    if (!fwError) statusByte = Wire.read();         // If fwError = 0 then a byte has been received
  }

  if (cmd == 'S') {
    if (statusByte < 0x10) Serial.print('0');     // Send Status Byte in hex
    Serial.print(statusByte, HEX);
  }
  return statusByte;                              // Return the CFW-9 Status byte
}
//==============================================================================================
byte isMoving() {
  fwError = 0;

  if (testMode) return (statusByte = ((millis() < timer) << 4) | (currentSlot = cmd ? currentSlot : 1)) & B01010000;
  getStatusByte();
  if (!fwError) fwError = (statusByte & B01000000) ? 1 : 0; // = 1 if wheel is stuck
  return (statusByte & B01010000);                          // If bits 4 or 6 is set then wheel is moving/stuck
  //  0 = Wheel at slot and not moving (i.e. OK)
  // 16 = Wheel still moving
  // 64 = Wheel arrived late or not at all
}
//==============================================================================================
byte waitForSlot() {
  byte moving;

  for (byte i = 1; i <= 32; i++) {                // Wait for up to 8 secs
    moving = isMoving();
    if (fwError) break;                           // Break if CFW-9 not repsonding
    if (!(moving & B1111000)) fwError = 0; break; // Wheel is at slot and not moving - all is good!
    fwError = 2;                                  // 2 = Wheel move timed out
    delay(240);
  }
  errorHandler(fwError);                          // Handle any error
  return fwError;                                 // 0 = OK, otherwise error
}
//==============================================================================================
byte getSlot() {
  return (waitForSlot()) ? 0 : slot();            // Wait for wheel and return slot or 0 for error
}
//==============================================================================================
byte slot() {
  return currentSlot = statusByte & B00001111;    // Get the current slot from bits 0-3 of the Status Byte
}
//==============================================================================================
/*
  CFW-9 I2C interface information
  ===============================

  CFW-9's I2C address is 52h.

  Move to slot commnand:  52h W (A4h) 10h 0Xh   where X is slot number 1 - 5, 0 forces movement to slot 1
  Get status byte:        52h R (A5h)           returns Status byte

  Status byte bits:

  0-3:  filter slot number (1 - 5, 0 if moving to home i.e. slot 1)
  4:    1 = moving, 0 = stationary (bests to ignore bit 0-3 if moving!)
  6:    0 = wheel is OK, 1 = wheel motion not detected in last 1-2 seconds(?) - probably stuck

  CFW-9's female DB9 connector pinout:

  3:    SCL (Serial Clock)  +3.3V - connected to NANO's SCL pin via logic level shifter 3.3V <> 5V
  4:    SDA (Serial Data)   +3.3V - connected to NANO's SDA pin via logic level shifter 3.3V <> 5V
  5:    Ground              0V
  8:    Power in            +12V (max 250mA, 10mA when idle) - powered by NANO's +5V USB input via voltage step-up converter to +10.5V

  Shell is connected to cable shielding (chassis ground).

  I2C clock speed: Standard 100KHz
*/