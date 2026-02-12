import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AnimalSprite from '../../app/components/AnimalSprite.vue'

describe('AnimalSprite Component', () => {
  const baseProps = {
    sprite: { id: '1', x: 100, y: 200, vx: 0, vy: 0, bobDelay: 0 },
    animal: { id: '1', name: 'Fluffy', kind: 'cat' as const },
    isSelected: false,
  }

  it('renders a button and an image', () => {
    const wrapper = mount(AnimalSprite, { props: baseProps })
    expect(wrapper.find('button').exists()).toBe(true)
    expect(wrapper.find('img').exists()).toBe(true)
  })

  it('applies selected class when isSelected is true', () => {
    const wrapper = mount(AnimalSprite, { props: { ...baseProps, isSelected: true } })
    expect(wrapper.find('.selected').exists()).toBe(true)
  })

  it('emits click event when button is clicked', async () => {
    const wrapper = mount(AnimalSprite, { props: baseProps })
    await wrapper.find('button').trigger('click')
    expect(wrapper.emitted('click')).toBeTruthy()
    expect(wrapper.emitted('click')?.[0]).toEqual(['1'])
  })

  it('applies facingLeft class when vx is negative', () => {
    const wrapper = mount(AnimalSprite, {
      props: { ...baseProps, sprite: { ...baseProps.sprite, vx: -5 } },
    })
    expect(wrapper.find('.facingLeft').exists()).toBe(true)
  })
})
