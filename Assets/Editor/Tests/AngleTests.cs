using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using SuperspectiveUtils;

public class AngleTests {

	[Test]
	public void IsClockwiseTest() {
		Angle startAngle = Angle.Degrees(15);
		Angle endAngle = Angle.Degrees(30);

		Assert.That(Angle.IsClockwise(startAngle, endAngle));
		Assert.That(!Angle.IsClockwise(endAngle, startAngle));

		startAngle = Angle.Degrees(345);
		endAngle = Angle.Degrees(15);

		Assert.That(Angle.IsClockwise(startAngle, endAngle));
		Assert.That(!Angle.IsClockwise(endAngle, startAngle));

		startAngle = Angle.Degrees(180);
		endAngle = Angle.Degrees(180);

		// No movement should not be considered clockwise
		Assert.That(!Angle.IsClockwise(startAngle, endAngle));
		Assert.That(!Angle.IsClockwise(endAngle, startAngle));
	}

	[Test]
	public void IsAngleBetweenTest() {
		Angle startAngle = Angle.Degrees(15);
		Angle endAngle = Angle.Degrees(30);

		Angle testAngleBetween = Angle.Degrees(20);
		Angle testAngleNotBetween = Angle.Degrees(190);

		Assert.That(Angle.IsAngleBetween(testAngleBetween, startAngle, endAngle));
		Assert.That(!Angle.IsAngleBetween(testAngleBetween, endAngle, startAngle));

		Assert.That(!Angle.IsAngleBetween(testAngleNotBetween, startAngle, endAngle));
		Assert.That(Angle.IsAngleBetween(testAngleNotBetween, endAngle, startAngle));

		startAngle = Angle.Degrees(345);
		endAngle = Angle.Degrees(15);

		testAngleBetween = Angle.Degrees(0);
		Assert.That(Angle.IsAngleBetween(testAngleBetween, startAngle, endAngle));
		Assert.That(!Angle.IsAngleBetween(testAngleBetween, endAngle, startAngle));

		testAngleBetween = Angle.Degrees(350);
		Assert.That(Angle.IsAngleBetween(testAngleBetween, startAngle, endAngle));
		Assert.That(!Angle.IsAngleBetween(testAngleBetween, endAngle, startAngle));

		testAngleBetween = Angle.Degrees(5);
		Assert.That(Angle.IsAngleBetween(testAngleBetween, startAngle, endAngle));
		Assert.That(!Angle.IsAngleBetween(testAngleBetween, endAngle, startAngle));

		testAngleNotBetween = Angle.Degrees(180);
		Assert.That(!Angle.IsAngleBetween(testAngleNotBetween, startAngle, endAngle));
		Assert.That(Angle.IsAngleBetween(testAngleNotBetween, endAngle, startAngle));
	}

	[Test]
	public void QuadrantTest() {
		Angle test = Angle.Degrees(0);
		Assert.That(test.quadrant == Angle.Quadrant.I);

		test = Angle.Degrees(15);
		Assert.That(test.quadrant == Angle.Quadrant.I);

		test = Angle.Degrees(90);
		Assert.That(test.quadrant == Angle.Quadrant.II);

		test = Angle.Degrees(105);
		Assert.That(test.quadrant == Angle.Quadrant.II);

		test = Angle.Degrees(180);
		Assert.That(test.quadrant == Angle.Quadrant.III);

		test = Angle.Degrees(195);
		Assert.That(test.quadrant == Angle.Quadrant.III);

		test = Angle.Degrees(270);
		Assert.That(test.quadrant == Angle.Quadrant.IV);

		test = Angle.Degrees(285);
		Assert.That(test.quadrant == Angle.Quadrant.IV);

		test = Angle.Degrees(360);
		Assert.That(test.quadrant == Angle.Quadrant.I);

		test = Angle.Degrees(-15);
		Assert.That(test.quadrant == Angle.Quadrant.IV);

		test = Angle.Degrees(450);
		Assert.That(test.quadrant == Angle.Quadrant.II);
	}
}
