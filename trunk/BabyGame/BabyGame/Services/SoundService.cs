// Copyright 2011 Murray Grant
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace MurrayGrant.BabyGame.Services
{
    public enum LongSoundOwner
    {
        None,
        MouseMovement,
        GamePadAnalogueInputs,
        Idle,
    }
    public class SoundService
    {
        private SoundEffectInstance[] _PlayingSoundsShort; 
        private SoundEffectInstance _PlayingSoundLong; 

        public GameMain Game { get; private set; }
        public LongSoundOwner LongSoundOwner { get; private set; }

        public SoundState LongSoundPlayingState
        {
            get
            {
                if (this._PlayingSoundLong == null)
                    return SoundState.Stopped;
                else
                    return this._PlayingSoundLong.State;
            }
        }
        
        public SoundService(GameMain game)
        {
            this.Game = game;
            this._PlayingSoundsShort = new SoundEffectInstance[4];  // Limit to 4 short sounds playing at once (from button presses).
            this._PlayingSoundLong = null;                          // Limit to a single long sound playing at once (from analogue inputs).
            this.LongSoundOwner = LongSoundOwner.None;
        }

        public void RemoveNonPlayingSounds()
        {
            for (int i = 0; i < this._PlayingSoundsShort.Length; i++)
            {
                if (this._PlayingSoundsShort[i] != null && this._PlayingSoundsShort[i].State != SoundState.Playing)
                {
                    this._PlayingSoundsShort[i].Stop();
                    this._PlayingSoundsShort[i].Dispose();
                    this._PlayingSoundsShort[i] = null;
                }
            }

            if (this._PlayingSoundLong != null && this._PlayingSoundLong.State != SoundState.Playing)
            {
                this._PlayingSoundLong.Stop();
                this._PlayingSoundLong.Dispose();
                this._PlayingSoundLong = null;
                this.LongSoundOwner = LongSoundOwner.None;
            }

        }
        public int TryPlaySoundButton(SoundEffect sound)
        {
            for (int i = 0; i < this._PlayingSoundsShort.Length; i++)
            {
                if (this._PlayingSoundsShort[i] == null)
                {
                    // Slot found to play a sound: add and play.
                    this._PlayingSoundsShort[i] = sound.CreateInstance();
                    this._PlayingSoundsShort[i].Play();
                    return i;
                }
            }
            return -1;
        }
        public bool TryPlaySoundLong(SoundEffect sound, LongSoundOwner owner)
        {
            if (owner == LongSoundOwner.None)
                throw new ArgumentOutOfRangeException("owner", "LongSoundOwner cannot be set to None.");

            if (this._PlayingSoundLong == null)
            {
                // Slot empty: add and play.
                this._PlayingSoundLong = sound.CreateInstance();
                this._PlayingSoundLong.Play();
                this.LongSoundOwner = owner;
                return true;
            }
            else
                return false;
        }

        public int ForcePlaySoundButton(SoundEffect sound)
        {
            // Try normal means first.
            var tryPlayResult = this.TryPlaySoundButton(sound);
            if (tryPlayResult != -1)
                return tryPlayResult;

            // Stop a random sound and play this one instead.
            var idx = this.Game.RandomGenerator.Next(0, this._PlayingSoundsShort.Length);
            this._PlayingSoundsShort[idx].Stop();
            this._PlayingSoundsShort[idx].Dispose();
            this._PlayingSoundsShort[idx] = sound.CreateInstance();
            this._PlayingSoundsShort[idx].Play();

            return idx;
        }
        public void ForcePlaySoundLong(SoundEffect sound, LongSoundOwner owner)
        {
            if (owner == LongSoundOwner.None)
                throw new ArgumentOutOfRangeException("owner", "LongSoundOwner cannot be set to None.");
            
            if (this._PlayingSoundLong == null && this._PlayingSoundLong.State == SoundState.Playing)
            {
                this._PlayingSoundLong.Stop();
                this._PlayingSoundLong.Dispose();
            }
            this._PlayingSoundLong = sound.CreateInstance();
            this._PlayingSoundLong.Play();
            this.LongSoundOwner = owner;
        }

        public void StopPlayingLongSound()
        {
            if (this._PlayingSoundLong != null)
            {
                this._PlayingSoundLong.Stop();
                this._PlayingSoundLong.Dispose();
                this._PlayingSoundLong = null;
                this.LongSoundOwner = LongSoundOwner.None;
            }
        }
        public void StopPlayingAllSounds()
        {
            for (int i = 0; i < this._PlayingSoundsShort.Length; i++)
            {
                if (this._PlayingSoundsShort[i] != null)
                {
                    this._PlayingSoundsShort[i].Stop();
                    this._PlayingSoundsShort[i].Dispose();
                    this._PlayingSoundsShort[i] = null;
                }
            }
            this.StopPlayingLongSound();
        }
    }

    public interface ISoundService
    {
        SoundService SoundService { get; }
    }

    public class SoundServiceContainer : ISoundService
    {
        public SoundService SoundService { get; private set; }

        internal SoundServiceContainer(SoundService soundService)
        {
            this.SoundService = soundService;
        }
    }
}
